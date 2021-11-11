using System.Diagnostics;
using essentialMix.Core.Web.Controllers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoPOC.UI.Models;

namespace MongoPOC.UI.Controllers
{
	public class HomeController : MvcController
	{
		/// <inheritdoc />
		public HomeController([NotNull] IConfiguration configuration, [NotNull] ILogger<HomeController> logger)
			: base(configuration, logger)
		{
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
