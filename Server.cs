using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Stripe;

namespace StripeService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
            .UseUrls("https://GarrettHite42.github.io/") //http://stripetest.dmns.org:4242/
            .UseWebRoot("public")
            .UseStartup<Startup>()
            .Build()
            .Run();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // This is your test secret API key.
            StripeConfiguration.ApiKey = "sk_test_npCC6YBiz4SxEnyzZF3WX4fH";

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseCors();
            app.UseCors(builder => builder.WithOrigins("4.4.78.195")
                .AllowAnyMethod()
                .AllowAnyHeader()
                // .AllowAnyOrigin()
				.SetIsOriginAllowed(origin => true)
				.AllowCredentials());
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}

[Route("yay")]
[ApiController]
public class YayController : Controller
{
    [HttpPost]
    public IActionResult Index()
    {
        Console.WriteLine("Yay");
        return Ok("Yay");
    }
}

[Route("create-payment-intent")]
[ApiController]
public class PaymentIntentApiController : Controller
{
    [HttpPost]
    public ActionResult Create(PaymentIntentCreateRequest request)
    {
        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
        {
            Amount = request.amount, //pass in amount
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                { "galaxy_stuff", "123456" },
                { "is_donation", "true"},
            }
        });

        return Json(new { clientSecret = paymentIntent.ClientSecret });
    }

    public class PaymentIntentCreateRequest
    {
        [JsonProperty("amount")]
        public int amount { get; set; }
    }
}

[Route("donation-succeeded")]
public class StripeWebHook : Controller
{
    const string endpointSecret = "whsec_b526d6368d2a76e20c442f562fe4292c3bd47d9f9b1edb6df7341014a7878a2a";

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);
            var data = stripeEvent.Data.Object;
            //var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);

            // Handle the event
            if (stripeEvent.Type == Events.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                //make a noise????????
                Console.WriteLine("PaymentIntent was successful!---------------------------------------------------------------");
            }
            else if (stripeEvent.Type == Events.PaymentMethodAttached)
            {
                var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                Console.WriteLine("PaymentMethod was attached to a Customer!");
            }
            // ... handle other event types
            else
            {
                Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
            }

            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }
}