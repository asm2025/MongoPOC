﻿using System;
using System.Net;
using essentialMix.Core.Web.Controllers;
using essentialMix.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MongoPOC.API.Controllers
{
	[AllowAnonymous]
	[Route("")]
	public class HomeController : ApiController
	{
		/// <inheritdoc />
		public HomeController(IConfiguration configuration, ILogger logger)
			: base(configuration, logger)
		{
		}

		[HttpGet]
		public IActionResult Index()
		{
			return Ok(Configuration.GetValue<string>("title"));
		}

		[HttpGet("[action]")]
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			Exception exception = HttpContext?.Features.Get<IExceptionHandlerPathFeature>().Error;
			if (exception == null) return Problem("Unknown error.", null, (int)HttpStatusCode.InternalServerError);

			ResponseStatus responseStatus = new ResponseStatus
			{
				StatusCode = (HttpStatusCode) HttpContext.Response.StatusCode,
				Exception = exception
			};

			return Problem(responseStatus.ToString(), null, HttpContext.Response.StatusCode);
		}
	}
}