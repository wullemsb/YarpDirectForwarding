using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;
using YarpDirectForwarding;

var builder = WebApplication.CreateBuilder(args);

var targetUrl = builder.Configuration.GetValue<string>("targetUrl");

builder.Services.AddHttpForwarder();
var app = builder.Build();

app.UseRouting();

//Always use HttpMessageInvoker rather than HttpClient, HttpClient buffers responses by default.
//Buffering breaks streaming scenarios and increases memory usage and latency.
var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false,
    ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current)
});


// Setup our own request transform class
var transformer = new CustomTransformer(); // or HttpTransformer.Default;
var requestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };

app.Map("/{**catch-all}", async (HttpContext httpContext, IHttpForwarder forwarder) => {
    var error = await forwarder.SendAsync(httpContext, targetUrl, httpClient, requestConfig,transformer);
    // Check if the operation was successful
    if (error != ForwarderError.None)
    {
        var errorFeature = httpContext.GetForwarderErrorFeature();
        var exception = errorFeature.Exception;
        //TODO: Log exception
    }
});

app.Run();







