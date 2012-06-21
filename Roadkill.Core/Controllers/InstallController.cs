﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Web.Security;
using System.Web.Management;
using System.Data.SqlClient;
using Roadkill.Core.Converters;
using Roadkill.Core.Search;
using System.IO;

namespace Roadkill.Core.Controllers
{
	/// <summary>
	/// Provides functionality for the installation wizard.
	/// </summary>
	/// <remarks>If the web.config "installed" setting is "true", then all the actions in
	/// this controller redirect to the homepage</remarks>
	public class InstallController : ControllerBase
    {
		/// <summary>
		/// Displays the start page for the installer (step1).
		/// </summary>
		public ActionResult Index()
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			return View("Step1");
		}

		/// <summary>
		/// Displays the second step in the installation wizard.
		/// </summary>
		public ActionResult Step2()
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			return View(new SettingsSummary());
		}

		/// <summary>
		/// Displays the authentication choice step in the installation wizard.
		/// </summary>
		/// <remarks>The <see cref="SettingsSummary"/> object that is POST'd is passed to the next step.</remarks>
		[HttpPost]
		public ActionResult Step3(SettingsSummary summary)
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			return View(summary);
		}

		/// <summary>
		/// Displays either the Windows Authentication settings view, or the DB settings view depending on
		/// the choice in Step3.
		/// </summary>
		/// <remarks>The <see cref="SettingsSummary"/> object that is POST'd is passed to the next step.</remarks>
		[HttpPost]
		public ActionResult Step3b(SettingsSummary summary)
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			summary.LdapConnectionString = "LDAP://";
			summary.EditorRoleName = "Editor";
			summary.AdminRoleName = "Admin";

			if (summary.UseWindowsAuth)
				return View("Step3WindowsAuth", summary);
			else
				return View("Step3Database",summary);
		}

		/// <summary>
		/// Displays the final installation step, which provides choices for caching, themes etc.
		/// </summary>
		/// <remarks>The <see cref="SettingsSummary"/> object that is POST'd is passed to the next step.</remarks>
		[HttpPost]
		public ActionResult Step4(SettingsSummary summary)
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			summary.AllowedExtensions = "jpg,png,gif,zip,xml,pdf";
			summary.AttachmentsFolder = "~/Attachments";
			summary.MarkupType = "Creole";
			summary.Theme = "Mediawiki";
			summary.CacheEnabled = true;
			summary.CacheText = true;

			return View(summary);
		}

		/// <summary>
		/// Validates the POST'd <see cref="SettingsSummary"/> object. If the settings are valid,
		/// an attempt is made to install using this.
		/// </summary>
		/// <returns>The Step5 view is displayed.</returns>
		[HttpPost]
		[ValidateInput(false)]
		public ActionResult Step5(SettingsSummary summary)
		{
			if (RoadkillSettings.Installed)
				return RedirectToAction("Index", "Home");

			try
			{
				// Any missing values are handled by data annotations. Those that are missed
				// can be seen as fiddling errors which are down to the user.

				if (ModelState.IsValid)
				{
					// Update the web.config first, so all connections can be referenced.
					SettingsManager.SaveWebConfigSettings(summary);

					// Create the roadkill schema and save the configuration settings
					SettingsManager.CreateTables(summary);
					SettingsManager.SaveSiteConfiguration(summary, true);	

					// Add a user if we're not using AD.
					if (!summary.UseWindowsAuth)
					{
						Install.AddAdminUser(summary);
					}					
	
					// Create a blank search index
					SearchManager.Current.CreateIndex();
				}
			}
			catch (Exception e)
			{
				try
				{
					Install.ResetInstalledState();
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("An error ocurred installing", ex.Message + e);
				}

				ModelState.AddModelError("An error ocurred installing", e.Message + e);
			}

			return View(summary);
		}

		//
		// JSON actions
		//

		/// <summary>
		/// This action is for JSON calls only. Attempts to contact an Active Directory server using the
		/// connection string and user details provided.
		/// </summary>
		/// <returns>Returns a <see cref="TestResult"/> containing information about any errors.</returns>
		public ActionResult TestLdap(string connectionString, string username, string password, string groupName)
		{
			if (RoadkillSettings.Installed)
				return Content("");

			string errors = Install.TestLdapConnection(connectionString, username, password, groupName);
			return Json(new TestResult(errors), JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// This action is for JSON calls only. Attempts to write to the web.config file and save it.
		/// </summary>
		/// <returns>Returns a <see cref="TestResult"/> containing information about any errors.</returns>
		public ActionResult TestWebConfig()
		{
			if (RoadkillSettings.Installed)
				return Content("");

			string errors = Install.TestSaveWebConfig();
			return Json(new TestResult(errors), JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// This action is for JSON calls only. Checks to see if the provided folder exists and if it can be written to.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns>Returns a <see cref="TestResult"/> containing information about any errors.</returns>
		public ActionResult TestAttachments(string folder)
		{
			if (RoadkillSettings.Installed)
				return Content("");

			string errors = Install.TestAttachments(folder);
			return Json(new TestResult(errors), JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// This action is for JSON calls only. Attempts a database connection using the provided connection string.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns>Returns a <see cref="TestResult"/> containing information about any errors.</returns>
		public ActionResult TestDatabaseConnection(string connectionString,string databaseType)
		{
			if (RoadkillSettings.Installed)
				return Content("");

			string errors = Install.TestConnection(connectionString, databaseType);
			return Json(new TestResult(errors), JsonRequestBehavior.AllowGet);
		}
    }

	/// <summary>
	/// Basic error information for the JSON actions
	/// </summary>
	public class TestResult
	{
		/// <summary>
		/// Any error message associated with the call.
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Indicates if there are any errors.
		/// </summary>
		public bool Success 
		{
			get { return string.IsNullOrEmpty(ErrorMessage); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TestResult"/> class.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		public TestResult(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}
	}
}
