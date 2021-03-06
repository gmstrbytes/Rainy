using System;
using System.IO;
using ServiceStack.Api.Swagger;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;


using Rainy.CustomHandler;
using Rainy.WebService;

namespace Rainy
{
	public delegate void ComposeObjectGraphDelegate (Funq.Container container);

	public class AppHost : AppHostHttpListenerBase
	{
		private ComposeObjectGraphDelegate ComposeObjectGraph;
		public AppHost (ComposeObjectGraphDelegate graph_composer) : base("Rainy", typeof(GetNotesRequest).Assembly)
		{
			this.ComposeObjectGraph = graph_composer;
		}

		public override void Configure (Funq.Container container)
		{
			this.ComposeObjectGraph (container);

			JsConfig.DateHandler = JsonDateHandler.ISO8601;

			Plugins.Add (new SwaggerFeature ());

			// register our custom exception handling
			this.ExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomExceptionHandler;
			this.ServiceExceptionHandler = Rainy.ErrorHandling.ExceptionHandler.CustomServiceExceptionHandler;

			string swagger_path;
			if (JsonConfig.Config.Global.Development)
			    swagger_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../swagger-ui/");
			else
				swagger_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "swagger-ui/");
			var swagger_handler = new FilesystemHandler ("/swagger-ui/", swagger_path);

			IHttpHandlerDecider uihandler;
			IHttpHandlerDecider fontshandler = null;
			if (JsonConfig.Config.Global.Development) {
				var webui_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../../Rainy.UI/dist/");
				uihandler = (IHttpHandlerDecider) new FilesystemHandler ("/", webui_path);
				var fonts_path = Path.Combine(Path.GetDirectoryName(this.GetType ().Assembly.Location), "../../../Rainy.UI/dist/fonts/");
				fontshandler = (IHttpHandlerDecider) new FilesystemHandler ("/fonts/", fonts_path);
			} else {
				uihandler = (IHttpHandlerDecider) new EmbeddedResourceHandler ("/", this.GetType ().Assembly, "Rainy.WebService.Admin.UI");
			}

			this.RequestFilters.Add ((req, resp, dto) => {
				if (req.HttpMethod == "OPTIONS") {
					resp.StatusCode = 200;
					resp.End ();
				}
			});

			// BUG HACK
			// GlobalResponseHeaders are not cleared between creating instances of a new config
			// this will be fatal (duplicate key error) for unit tests so we clear the headers
			EndpointHostConfig.Instance.GlobalResponseHeaders.Clear ();

			var endpoint_config = new EndpointHostConfig {

				EnableFeatures = Feature.All.Remove (Feature.Metadata),
				//DefaultRedirectPath = "/admin/",

				// not all tomboy clients send the correct content-type
				// so we force application/json
				DefaultContentType = ContentType.Json,

				RawHttpHandlers = { 
					uihandler.CheckAndProcess,
					fontshandler.CheckAndProcess,
					swagger_handler.CheckAndProcess
				},

				// enable cors
				GlobalResponseHeaders = {
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
					// time in seconds preflight responses can be cached by the client
					{ "Access-Control-Max-Age", "1728000" },
//					{ "Access-Control-Max-Age", "1" },

					// the Authority header must be whitelisted; it is sent be the rainy-ui
					// for authentication
					{ "Access-Control-Allow-Headers", "Content-Type, Authority, AccessToken" },
				}, 
			};
			endpoint_config.AddMaxAgeForStaticMimeTypes["text/html"] = new TimeSpan (1, 0, 0);
			endpoint_config.AddMaxAgeForStaticMimeTypes["text/css"] = new TimeSpan (1, 0, 0);
			endpoint_config.AddMaxAgeForStaticMimeTypes["text/javascript"] = new TimeSpan (1, 0, 0);
			SetConfig (endpoint_config);
		}
	}
}