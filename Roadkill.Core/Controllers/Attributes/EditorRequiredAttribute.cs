﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace Roadkill.Core
{
	/// <summary>
	/// Represents an attribute that is used to restrict access by callers to users that are in Editor role group.
	/// </summary>
	public class EditorRequiredAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Provides an entry point for custom authorization checks.
		/// </summary>
		/// <param name="httpContext">The HTTP context, which encapsulates all HTTP-specific information about an individual HTTP request.</param>
		/// <returns>
		/// true if the user is an admin, in the role name specified by the roadkill web.config editorRoleName setting or if this is blank; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="httpContext"/> parameter is null.</exception>
		protected override bool AuthorizeCore(HttpContextBase httpContext)
		{
			IPrincipal user = httpContext.User;
			IIdentity identity = user.Identity;

			if (!identity.IsAuthenticated)
			{
				return false;
			}

			// An empty editor role name implies everyone is an editor - there's no page security.
			if (string.IsNullOrEmpty(RoadkillSettings.EditorRoleName))
				return true;

			if (UserManager.Current.IsAdmin(identity.Name) || UserManager.Current.IsEditor(identity.Name))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
