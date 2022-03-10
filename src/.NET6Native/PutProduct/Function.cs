using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Shared.DataAccess;
using Shared.Models;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<ApiGatewayProxyJsonSerializerContext>))]

AWSSDKHandler.RegisterXRayForAllServices();
ProductsDAO dataAccess = new DynamoDbProducts();

var handler = async (APIGatewayHttpApiV2ProxyRequest apigProxyEvent, ILambdaContext context) =>
{
    if (!apigProxyEvent.RequestContext.Http.Method.Equals(HttpMethod.Put.Method))
    {
        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = "Only PUT allowed",
            StatusCode = (int)HttpStatusCode.MethodNotAllowed,
        };
    }
    
    try
    {
        var id = apigProxyEvent.PathParameters["id"];

        var product = JsonSerializer.Deserialize<Product>(apigProxyEvent.Body);

        if (product == null || id != product.Id)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                Body = "Product ID in the body does not match path parameter",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

        await dataAccess.PutProduct(product);
        
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = (int)HttpStatusCode.Created,
            Body = $"Created product with id {id}"
        };
    }
    catch (Exception e)
    {
        context.Logger.LogError($"Error creating product {e.Message} {e.StackTrace}");
        
        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = "Not Found",
            StatusCode = (int)HttpStatusCode.InternalServerError,
        };
    }
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
public partial class ApiGatewayProxyJsonSerializerContext : JsonSerializerContext
{
}
