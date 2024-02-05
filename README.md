## Aptean Inegration Platform (AIP)
This document describes API usage for v1 and v2. Note that v2 details are currently being added.

*Swagger Documentation*: https://stg.integration-graph.apteansharedservices.com/swagger/index.html

## API headers
API use requires subscribing to AIP platform. These are the standard authorization headers. 
```
X-APTEAN-TENANT : tenant ID
X-APTEAN-APIM : API Key
X-APTEAN-PRODUCT : product ID
X-APTEAN-CORRELATION-ID : correlation ID to group published event and consumer logs and any system events
```

**Producer - publish events**:

| **Tasks**                  | **API**                |
| :------------------------- | :--------------------- |
| Register event definitions | POST event-definitions |
| Publish event              | POST events            |
|                            |                        |

**Consumer**: (requires Webhook to receive events*)

| **Tasks**            | **API**        |
| :------------------- | :------------- |
| Register as consumer | POST consumers |
| Log processing steps | POST events/eventlog |
|                      |                |

**Playground**: You can use the Postman collection (needs to be updated for v2) and environment setup json to invoke the APIs. Plugin the subscription info in the environment setup.

**Webhook receiver if you are consuming events**:
You can use the following endpoint as sample webhook reveiver https://stg.integration-consumer.apteansharedservices.com/v1/webhook/{{guid}} 
(substitute the GUID to create your own unique inbox). You can then view the events in https://stg.integration-consumer.apteansharedservices.com/inbox
Or use webhook.site which is publicly available site to quickly set up webhook endpoint.

For actual implementation you should create your own http service. See the following template for guidance.

For the http endpoint to be registered as Webhook receiver for AIP you can implement the following in the API controller. (You could also build a logic app/function app for a webhook receiver. More information is available in Microsoft sites for Event Grid).

Webhook service must perform the following actions:
- Respond to validation event so AIP can send events to the webhook - this action is called only when the consumer is registered the first time
- Handle the event and acknowledge with http response code 200
- Call the end point **/events/eventlog** to log all pertinent actions asoociated with processing the event - these logs are used for event visualization

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleReveiver
{
    [Route("api/[controller]")]
    public class WebhookController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                // Check the event type.
                // Return the validation code if it's 
                // a subscription validation request. 
                if (EventTypeSubcriptionValidation)
                {
                    return await HandleValidation(jsonContent);
                }
                else if (EventTypeNotification)
                {
                    return await HandleGridEvents(jsonContent);
                }            
                return BadRequest();                
            }
        }

        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
                "SubscriptionValidation";
        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
                "Notification";

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            var gridEvent =
                JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                    .First();
            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var events = JArray.Parse(jsonContent);
            foreach (var e in events)
            {
                var details = JsonConvert.DeserializeObject<GridEvent<dynamic>>(e.ToString());
                //validate payload signature -- see sample project
                //process event and
                //call eventlog for each step using the X-APTEAN-CORRELATION-ID
            }

            return Ok();
        }

        public class GridEvent<T> where T: class
        {
            public string Id { get; set;}
            public string EventType { get; set;}
            public string Subject {get; set;}
            public DateTime EventTime { get; set; } 
            public T Data { get; set; } 
            public string Topic { get; set; }
        }
    }
}
```
