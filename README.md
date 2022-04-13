API helpers, Postman collection, etc.

## Aptean Inegration Platform (AIP)
API use requires subscribing to AIP platform. (Requests are handled manually at this point)
```
Product ID: {}
Tenant ID: {}
Secret: {}
API Key Server: {}
API Key Browser: {}
```
Documentation: https://stg.integration-graph.apteansharedservices.com/swagger/index.html

**Producer**:

| **Tasks**                  | **API**                |
| :------------------------- | :--------------------- |
| Register as producer       | POST producers         |
| Register event definitions | POST event-definitions |
| Publish event              | POST events            |
|                            |                        |

**Consumer**: (requires Webhook to receive events*)

| **Tasks**            | **API**        |
| :------------------- | :------------- |
| Register as consumer | POST consumers |
|                      |                |

**Playground**: You can use the Postman collection and environment setup json to invoke the APIs. Plugin the subscription info in the environment setup.

**Webhook receiver**:
You can use this endpoint as sample webhook reveiver https://stg.integration-consumer.apteansharedservices.com/v1/webhook/1be0959e-5e1a-4053-b273-2759efa045bf 
(substitute the GUID to create your own unique inbox)

You can then view the events in https://stg.integration-consumer.apteansharedservices.com/inbox

For the http endpoint to be registered as Webhook receiver for AIP you need to implement the following in the API controller:

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
            return BadRequest();                
        }
    }

    private bool EventTypeSubcriptionValidation
        => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
            "SubscriptionValidation";

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
```