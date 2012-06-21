﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Roadkill.Core.Converters;
using System.Web;
using System.Text.RegularExpressions;
using Recaptcha;
using System.Web.UI;
using System.IO;

namespace Roadkill.Core
{
	/// <summary>
	/// Roadkill specific extensions methods for the <see cref="HtmlHelper"/> class.
	/// </summary>
	public static class HtmlExtensions
	{
		/// <summary>
		/// Turns a tag string into a HTML tag cloud, which produces different spans based on the
		/// number of pages with each tag in the system.
		/// </summary>
		/// <param name="content">The tags in ";" delimited format.</param>
		/// <returns>A HTML tag cloud.</returns>
		public static MvcHtmlString TagBlocks(this HtmlHelper helper, string content)
		{
			string result = "";

			if (!string.IsNullOrWhiteSpace(content))
			{
				string[] parts = content.Split(';');

				StringBuilder builder = new StringBuilder();
				foreach (string item in parts)
				{
					if (!string.IsNullOrWhiteSpace(item))
					{
						string url = helper.ActionLink(item, "Tag", "Pages", new { id = item },null).ToString();
						builder.AppendFormat("<span class=\"tagblock\">{0}</span>", url);
					}
				}

				result = builder.ToString();
			}

			return MvcHtmlString.Create(result);
		}

		/// <summary>
		/// Removes all bad characters (ones which cannot be used in a URL for a page) from a page title.
		/// </summary>
		/// <param name="title">The page title.</param>
		/// <returns>The cleaned title</returns>
		public static string EncodeTitle(this string title)
		{
			return EncodeTitleInternal(title);
		}

		/// <summary>
		/// Removes all bad characters (one which cannot be used in a URL for a page) from a page title.
		/// </summary>
		/// <param name="title">The page title.</param>
		/// <returns>The cleaned title</returns>
		public static MvcHtmlString EncodeTitle(this UrlHelper helper, string title)
		{
			title = EncodeTitleInternal(title);
			return MvcHtmlString.Create(title);
		}

		private static string EncodeTitleInternal(string title)
		{
			if (string.IsNullOrEmpty(title))
				return title;

			// Search engine friendly slug routine with help from http://www.intrepidstudios.com/blog/2009/2/10/function-to-generate-a-url-friendly-string.aspx
			
			// remove invalid characters
			title = Regex.Replace(title, @"[^\w\d\s-]", "");  // this is unicode safe, but may need to revert back to 'a-zA-Z0-9', need to check spec
			
			// convert multiple spaces/hyphens into one space       
			title = Regex.Replace(title, @"[\s-]+", " ").Trim(); 
			
			// If it's over 30 chars, take the first 30.
			title = title.Substring(0, title.Length <= 75 ? title.Length : 75).Trim(); 
			
			// hyphenate spaces
			title = Regex.Replace(title, @"\s", "-");

			return title;
		}

		/// <summary>
		/// Turns the provided wiki markdown into HTML, using the current markdown syntax parser.
		/// </summary>
		/// <param name="content">The wiki markdown.</param>
		/// <returns>The converted HTML.</returns>
		public static MvcHtmlString WikiMarkupToHtml(this HtmlHelper helper, string content)
		{
			return MvcHtmlString.Create(content.WikiMarkupToHtml());
		}

		/// <summary>
		/// Gets a CSS class name for the tag based on the <see cref="TagSummary.Count"/> - the number of
		/// pages with that tag in the system.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="tag">A <see cref="TagSummary"/>.</param>
		/// <returns>
		/// <list type="bullet">
		/// <item>1 tag: "tagcloud1"</item>
		/// <item>1-3 tags: "tagcloud2"</item>
		/// <item>3-5 tags: "tagcloud3"</item>
		/// <item>5-10 tags: "tagcloud4"</item>
		/// <item>10+ tags: "tagcloud5"</item>
		/// </list>
		/// </returns>
		public static string ClassNameForTagSummary(this HtmlHelper helper, TagSummary tag)
		{
			string className = "";

			if (tag.Count > 10)
			{
				className = "tagcloud5";
			}
			else if (tag.Count >= 5 && tag.Count < 10)
			{
				className = "tagcloud4";
			}
			else if (tag.Count >= 3 && tag.Count < 5)
			{
				className = "tagcloud3";
			}
			else if (tag.Count > 1 && tag.Count < 3)
			{
				className = "tagcloud2";
			}
			else if (tag.Count == 1)
			{
				className = "tagcloud1";
			}

			return className;
		}

		/// <summary>
		/// Gets a complete URL path to an item in the current theme directory.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="relativePath">The filename or path inside the current theme directory.</param>
		/// <returns>A url path to the item, e.g. '/MySite/Themes/Mediawiki/logo.png'</returns>
		public static string ThemeContent(this UrlHelper helper, string relativePath)
		{
			return helper.Content(RoadkillSettings.ThemePath + "/" + relativePath);
		}

		/// <summary>
		/// Formats a number in bytes using KB or bytes if it is less than 1024 bytes.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="size">The size in bytes.</param>
		/// <returns>If the size parameter is 900: 900 bytes. If size is 4000: 4KB.</returns>
		public static string FormatFileSize(this HtmlHelper helper,int size)
		{
			if (size > 1024)
				return size / 1024 + "KB";
			else
				return size + " bytes";
		}

		/// <summary>
		/// Renders the Recaptcha control as HTML, if recaptcha is enabled.
		/// </summary>
		public static MvcHtmlString RenderCaptcha(this HtmlHelper helper)
		{
			if (RoadkillSettings.IsRecaptchaEnabled)
			{
				RecaptchaControl control = new RecaptchaControl();
				control.ID = "recaptcha";
				control.Theme = "clean";
				control.PublicKey = RoadkillSettings.RecaptchaPublicKey;
				control.PrivateKey = RoadkillSettings.RecaptchaPrivateKey;

				using (StringWriter stringWriter = new StringWriter())
				{
					using (HtmlTextWriter htmlWriter = new HtmlTextWriter(stringWriter))
					{
						stringWriter.WriteLine("<br/><label class=\"userlabel\">As an anti-spam measure, please enter the two words below</label><br style=\"clear:both\"><br/>");
						control.RenderControl(htmlWriter);
						stringWriter.WriteLine("<br/>");
						
						return MvcHtmlString.Create(htmlWriter.InnerWriter.ToString());
					}
				}
			}
			else
			{
				return MvcHtmlString.Empty;
			}
		}

		/// <summary>
		/// Displays a drop list for the DatabaseType enum, using the currently selected value from RoadkillSettings.DatabaseType
		/// </summary>
		public static MvcHtmlString DropDownBoxForDatabaseType(this HtmlHelper helper)
		{
			List<SelectListItem> selectList = new List<SelectListItem>();

			// These are hardcoded as there will never be (except perhaps Oracle) more than the list of datatypes below

			SelectListItem listItem = new SelectListItem() { Text = "DB2", Value = DatabaseType.DB2.ToString() };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "Firebird", Value = DatabaseType.Firebird.ToString() };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "MySQL", Value = DatabaseType.MySQL.ToString() };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "Postgres", Value = DatabaseType.Postgres.ToString() };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "Sqlite", Value = DatabaseType.Sqlite.ToString()};
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "SqlServer 2005", Value = DatabaseType.SqlServer2005.ToString(), Selected=true };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "SqlServer 2008", Value = DatabaseType.SqlServer2008.ToString() };
			selectList.Add(listItem);

			listItem = new SelectListItem() { Text = "Sql Server CE", Value = DatabaseType.SqlServerCe.ToString() };
			selectList.Add(listItem);		

			return helper.DropDownList("DatabaseType", selectList);
		}
	}
}
