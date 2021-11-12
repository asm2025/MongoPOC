using System;
using System.Net;
using essentialMix.Core.Web.Controllers;
using essentialMix.Extensions;
using essentialMix.Web;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MongoPOC.API.Controllers
{
	[AllowAnonymous]
	[Route("")]
	public class HomeController : ApiController
	{
		/// <inheritdoc />
		public HomeController([NotNull] IConfiguration configuration, [NotNull] ILogger<HomeController> logger)
			: base(configuration, logger)
		{
		}

		[HttpGet]
		public IActionResult Index()
		{
			return Environment.IsDevelopment()
						? Ok(Configuration.GetValue<string>("title"))
						: NotFound();
		}

		[Route("[action]/{id:int?}")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error(int? id)
		{
			Exception exception = HttpContext?.Features.Get<IExceptionHandlerPathFeature>()?.Error;
			if (exception != null) Logger.LogError(exception.CollectMessages());
			id ??= (int)HttpStatusCode.InternalServerError;
			
			ResponseStatus responseStatus = new ResponseStatus
			{
				StatusCode = (HttpStatusCode)id,
				Exception = exception
			};

			return Problem(responseStatus.ToString(), null, id);
		}
	}
}