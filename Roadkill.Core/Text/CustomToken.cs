﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace Roadkill.Core
{
	/// <summary>
	/// Represents a single token for text replacement inside the wiki markup.
	/// </summary>
	public class TextToken
	{
		/// <summary>
		/// The name of the token, for use as in-line help.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The description of the token, for use as in-line help.
		/// </summary>
		public string Description { get; set; }
		
		/// <summary>
		/// The regex to search the text with. This is a single line, compiled regex.
		/// </summary>
		public string SearchRegex { get; set; }

		/// <summary>
		/// The HTML replacement for the search regex.
		/// </summary>
		public string HtmlReplacement { get; set; }
	}
}
