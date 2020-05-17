using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FTS.AWS.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace FTS.AWS.API.Publish.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PublishMessageController : ControllerBase
    {
        IConfiguration _configuration;
        private readonly ILogger<PublishMessageController> _logger;

        private readonly IHttpClientFactory _httpClientFactory;
        private IAmazonSimpleNotificationService _simpleNotificationService;

        public PublishMessageController(ILogger<PublishMessageController> logger, IConfiguration configuration,
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

            sample.Add("publish1");
            sample.Add("running1");
            return sample;
        }

        [HttpPost("MultiplePublish")]
        public async Task<IActionResult> Publish(publishModel publishModel)
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

                if (publishModel == null || publishModel.TotalRecordAdd == 0)
                {
                    throw new Exception("Invalid request");
                }

                for (int x = 1; x <= publishModel.TotalRecordAdd; x++)
                {
                    Contact contact = new Contact
                    {
                        CustomeId = x,
                        Name = publishModel.Contact.Name,
                        Address = publishModel.Contact.Address,
                        Email = x + publishModel.Contact.Email,
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
                return this.Ok("Successfully publish");
            }

            catch (AmazonSimpleNotificationServiceException ex)
            {
                _logger.LogError(ex, "An AWS SNS exception was thrown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception was thrown");
            }
            return this.Ok("failed to publish");
        }
    }

    public class publishModel
    {
        public int TotalRecordAdd { get; set; }

        public Contact Contact { get; set; }
    }
}