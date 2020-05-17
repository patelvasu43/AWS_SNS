using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleNotificationService.Util;
using FTX.Kafka.Integration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FTS.AWS.API.Core.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class AWS1Controller : ControllerBase
    {
        IConfiguration _configuration;
        private readonly ILogger<AWS1Controller> _logger;

        private readonly IHttpClientFactory _httpClientFactory;
        private IAmazonSimpleNotificationService _simpleNotificationService;

        public AWS1Controller(ILogger<AWS1Controller> logger, IConfiguration configuration,
            IHttpClientFactory httpClientFactory
            )
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            List<string> sample = new List<string>();

            sample.Add("working1");
            sample.Add("running1");
            return sample;
        }

        [HttpGet("MultiplePublish")]
        public async Task Publish(int sentMessagecounter)
        {
            try
            {
                string accessKey = _configuration.GetSection("AWS").GetValue<string>("awsAccessKeyId");
                string secretKey = _configuration.GetSection("AWS").GetValue<string>("awsSecretAccessKey");
                string regionString = _configuration.GetSection("AWS").GetValue<string>("Region");
                RegionEndpoint region = RegionEndpoint.GetBySystemName(regionString);
                //var snsClient = new AmazonSimpleNotificationServiceClient(accessKey, secretKey, region);
                string topicName = _configuration.GetValue<string>("TopicName");
                _simpleNotificationService = new AmazonSimpleNotificationServiceClient(accessKey, secretKey, region);


                Topic topic = await _simpleNotificationService.FindTopicAsync(topicName);

                for (int x = 1; x <= sentMessagecounter; x++)
                {
                    Contact contact = new Contact
                    {
                        CustomeId = x,
                        Name = "vasu",
                        Address = "test123",
                        Email = x + "vasumca82@gmail.com",
                    };


                    string serializedContact = JsonConvert.SerializeObject(contact);
                    var request = new PublishRequest(topic.TopicArn, serializedContact);
                    var response = await _simpleNotificationService.PublishAsync(request);

                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        _logger.LogInformation($"Successfully sent SNS message '{response.MessageId}  and Id={ x} '");
                    }
                    else
                    {
                        _logger.LogWarning(
                            $"Received a failure response '{response.HttpStatusCode}' when sending SNS message '{response.MessageId ?? "Missing ID"}'");
                    }
                }
            }

            catch (AmazonSimpleNotificationServiceException ex)
            {
                _logger.LogError(ex, "An AWS SNS exception was thrown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception was thrown");
            }
        }

        [HttpPost("Subscribe")]
        public async Task<IActionResult> Subscribe()
        {
            string message;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                message = await reader.ReadToEndAsync();
            }
            _logger.LogInformation($"Request data : {message}");

            bool isValidRequest = _configuration.GetValue<bool>("ValidRequest");
            if (!isValidRequest)
            {
                throw new  Exception("Invalid Data");
                //return HttpStatusCode.InternalServerError
                //return StatusCode((int)HttpStatusCode.GatewayTimeout);
                //return StatusCode((int)HttpStatusCode.InternalServerError);
            }


            try
            {


                var snsMessage = Message.ParseMessage(message);
                bool isValid = snsMessage.IsMessageSignatureValid();

                _logger.LogInformation($"is valid request {isValid } ");

                if (isValid)
                {
                    if (snsMessage.IsSubscriptionType)
                    {
                        string Status = await SubscribeConfirmmation(snsMessage);
                        _logger.LogInformation(Status);
                    }
                    else if (snsMessage.IsNotificationType)
                    {
                        NotificationMessage(snsMessage);
                        _logger.LogInformation($"Notification Success for {snsMessage}");
                    }
                }
                else
                {
                    throw new Exception("Invalid signature of the request.");
                }

                return this.Ok(message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, exception: ex, "error occured");
                throw;
            }
        }

        public async Task<string> SubscribeConfirmmation(Message snsMessage)
        {
            //using (var webClient = new WebClient())
            //{
            //    //var subscribeUri = new Uri(snsMessage.SubscribeURL);
            //    return await webClient.DownloadStringTaskAsync(snsMessage.SubscribeURL);
            //}
            var client = _httpClientFactory.CreateClient();
            var result = await client.GetStringAsync(snsMessage.SubscribeURL);
            //return new OkObjectResult("Confirmed");
            return result;
        }

        public async void NotificationMessage(Message snsMessage)
        {
            Contact contactTostore = JsonConvert.DeserializeObject<Contact>(snsMessage.MessageText);
            using (TestEntityFContext context = new TestEntityFContext())
            {
                contactTostore.Email = "aws1:" + contactTostore.Email;
                context.Contact.Add(contactTostore);
                await context.SaveChangesAsync();
            }
        }
    }
}