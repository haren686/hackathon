using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.AspNetCore.Mvc;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Hackathon.Web.Models;

namespace Hackathon.Web.Controllers
{
    public class CreditCardController : Controller
    {
        public async Task<IActionResult> CompareCreditCard(string Id)
        {
            try
            {
                if (String.IsNullOrEmpty(Id)) return RedirectToAction("Error", "Home");

                NewCardDetails newCardDetails = null;
                bool initialized = false;

                using (HttpClient client = new HttpClient())


                {
                    string custDetails = await client.GetStringAsync("https://kq813thfo9.execute-api.us-west-2.amazonaws.com/dev/customerdetails?guid=" + Id);

                    CustomerDetails custInfo = JsonConvert.DeserializeObject<CustomerDetails>(custDetails);

                    if (custInfo != null && custInfo.statusCode == 200)
                    {
                        string proposedCardDetails = await client.GetStringAsync("https://l9zwpwmshi.execute-api.us-west-2.amazonaws.com/dev/getcardbenefits?customerId=" + custInfo.body.CustomerDetails.CustomerId);
                        TempData["proposedCardDetails"] = proposedCardDetails;
                        TempData["UserInfo"] = custDetails;
                        newCardDetails = JsonConvert.DeserializeObject<NewCardDetails>(proposedCardDetails);
                    }
                    if (newCardDetails != null && newCardDetails.statusCode == 200) initialized = true;

                    TempData["CustDetails"] = custInfo;
                    TempData["NewCardDetails"] = newCardDetails;
                }
                if (initialized)
                    return View();
                else
                    return RedirectToAction("Error", "Home");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult ApplyCreditCard()
        {
            try
            {
                CustomerDetails custInfo = JsonConvert.DeserializeObject<CustomerDetails>(TempData["UserInfo"].ToString());
                NewCardDetails newCardDetails = JsonConvert.DeserializeObject<NewCardDetails>(TempData["proposedCardDetails"].ToString());
                TempData["CustDetails"] = custInfo;
                TempData["newCardDetails"] = newCardDetails;
                TempData.Keep("UserInfo");
                TempData.Keep("proposedCardDetails");
                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> UploadAndAuthenticate()
        {
            try
            {
                NewCardDetails newCardDetails = JsonConvert.DeserializeObject<NewCardDetails>(TempData["proposedCardDetails"].ToString());
                CustomerDetails custInfo = JsonConvert.DeserializeObject<CustomerDetails>(TempData["UserInfo"].ToString());
                byte[] bytes = new byte[Request.Form.Files[0].Length];
                using (var reader = Request.Form.Files[0].OpenReadStream())
                {
                    await reader.ReadAsync(bytes, 0, (int)Request.Form.Files[0].Length);
                }

                if (await AuthenticateUserByFace(bytes))
                {
                    AmazonSQSClient sqsClient = new AmazonSQSClient(Environment.GetEnvironmentVariable("ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("SECRET_ACCESS_KEY"), RegionEndpoint.USEast2);
                    var request = new SendMessageRequest
                    {
                        DelaySeconds = (int)TimeSpan.FromSeconds(5).TotalSeconds,
                        MessageAttributes = new Dictionary<string, MessageAttributeValue>
                                        {
                                            {"CustId", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue = Convert.ToInt32(custInfo.body.CustomerDetails.CustomerId).ToString()
                                                }
                                            },
                                            {"NameOnCard", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue = custInfo.body.CardInfo.NameOnCard.ToString()
                                                }
                                            },
                                            {"NewCardId", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue  = Convert.ToInt32(newCardDetails.body.CardDetails.CardTypeId).ToString()
                                                }
                                            },
                                            {"CardSegment", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue  = newCardDetails.body.CardDetails.CardSegment.ToString()
                                                }
                                            },
                                            {"CreditLimit", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue  = Convert.ToInt32(custInfo.body.CardInfo.CreditLimit + 10000).ToString()
                                                }
                                            },
                                             {"ExpiryDate", new MessageAttributeValue
                                                {
                                                    DataType = "String", StringValue  = DateTime.Now.AddYears(5).ToString()
                                                }
                                            }
                                        },
                        MessageBody = $"User {custInfo.body.CustomerDetails.CustomerId + " - " + custInfo.body.CustomerDetails.Name } has been authenticated successfully",
                        QueueUrl = "https://sqs.us-east-2.amazonaws.com/538588550648/ProcessCreditCardRequest"
                    };

                    SendMessageResponse response = await sqsClient.SendMessageAsync(request);
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                        return RedirectToAction("Success");
                    else
                        return RedirectToAction("Fail");
                }
                else
                    return RedirectToAction("Fail");
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<bool> AuthenticateUserByFace(byte[] targetImage)
        {
            try
            {
                float similarityThreshold = 90F;
                CustomerDetails custInfo = JsonConvert.DeserializeObject<CustomerDetails>(TempData["UserInfo"].ToString());
                string sourceImage = custInfo.body.CustomerDetails.ImageUrl;

                AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient(Environment.GetEnvironmentVariable("ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("SECRET_ACCESS_KEY"), RegionEndpoint.USWest2);

                Image imageSource = new Image();
                var webClient = new WebClient();
                byte[] imageBytes = webClient.DownloadData(sourceImage);
                imageSource.Bytes = new MemoryStream(imageBytes);

                Image imageTarget = new Image();
                imageTarget.Bytes = new MemoryStream(targetImage);
                CompareFacesRequest compareFacesRequest = new CompareFacesRequest()
                {
                    SourceImage = imageSource,
                    TargetImage = imageTarget,
                    SimilarityThreshold = similarityThreshold
                };

                // Call operation
                CompareFacesResponse compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);

                if (compareFacesResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    if (compareFacesResponse.FaceMatches.Count > 0 && compareFacesResponse.FaceMatches.Count < 2)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new Exception();
            }
        }

        public IActionResult Success()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }
        public IActionResult Fail()
        {
            try
            {
                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }





    }
}