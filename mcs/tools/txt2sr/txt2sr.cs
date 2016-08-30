//
// txt2sr.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2016 Xamarin Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;
using Mono.Options;

public class Program
{
	class CmdOptions
	{
		public bool ShowHelp { get; set; }
		public bool Verbose { get; set; }
		public List<string> ResourcesStrings { get; }
		public bool IgnoreSemicolon { get; set; }

		public CmdOptions ()
		{
			ResourcesStrings = new List<string> ();
		}
	}

	public static int Main (string[] args)
	{
		var options = new CmdOptions ();

		var p = new OptionSet () {
			{ "t|txt=", "File with string resource in key=value format",
				v => options.ResourcesStrings.Add (v) },
			{ "h|help",  "Display available options", 
				v => options.ShowHelp = v != null },
			{ "v|verbose",  "Use verbose output", 
				v => options.Verbose = v != null },
			{ "ignore-semicolon", "Reads lines starting with semicolon",
				v => options.IgnoreSemicolon = v != null },
		};

		List<string> extra;
		try {
			extra = p.Parse (args);
		}
		catch (OptionException e) {
			Console.WriteLine (e.Message);
			Console.WriteLine ("Try 'txt2sr -help' for more information.");
			return 1;
		}

		if (options.ShowHelp) {
			ShowHelp (p);
			return 0;
		}

		if (extra.Count != 1) {
			ShowHelp (p);
			return 2;
		}

		var txtStrings = new List<Tuple<string, string>> ();
		if (!LoadStrings (txtStrings, options))
			return 3;

		GenerateFile (extra [0], txtStrings, options);

		return 0;
	}

	static void ShowHelp (OptionSet p)
	{
		Console.WriteLine ("Usage: cil-txt2sr [options] output-file");
		Console.WriteLine ("Generates C# file from reference source resource text file");
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		p.WriteOptionDescriptions (Console.Out);
	}

	static void GenerateFile (string outputFile, List<Tuple<string, string>> txtStrings, CmdOptions options)
	{
		using (var str = new StreamWriter (outputFile)) {
			str.WriteLine ("//");
			str.WriteLine ("// This file was generated by txt2sr tool");
			str.WriteLine ("//");
			str.WriteLine ();

			str.WriteLine ("partial class SR");
			str.WriteLine ("{");
			foreach (var entry in txtStrings) {
				var value = entry.Item2;

				if (value.StartsWith ("\"") && value.EndsWith ("\";")) {
					value = value.Substring (1, value.Length - 3);
				}

				int idx;
				int startIndex = 1;
				while (startIndex <= value.Length && (idx = value.IndexOf ("\"", startIndex, StringComparison.Ordinal)) > 0) {
					startIndex = idx + 1;

					if (value [idx - 1] == '\\')
						continue;
					
					value = value.Insert (idx, "\\");
					++startIndex;
				}

				str.WriteLine ($"\tpublic const string {entry.Item1} = \"{value}\";");
			}
			str.WriteLine ("}");
		}
	}

	static bool LoadStrings (List<Tuple<string, string>> resourcesStrings, CmdOptions options)
	{
		var keys = new Dictionary<string, string> ();
		foreach (var fileName in options.ResourcesStrings) {
			if (!File.Exists (fileName)) {
				Console.Error.WriteLine ($"Error reading resource file '{fileName}'");
				return false;
			}

			foreach (var l in File.ReadLines (fileName)) {
				var line = l.Trim ();
				if (line.Length == 0 || line [0] == '#')
					continue;

				int start = 0;
 				if (line [0] == ';') {
 					if (!options.IgnoreSemicolon)
 						continue;

 					start = 1;
 				}

				var epos = line.IndexOf ('=');
				if (epos < 0)
					continue;

				var key = line.Substring (start, epos - start).Trim ();
				if (key.Contains (" "))
					continue;

				var value = line.Substring (epos + 1).Trim ();

				string existing;
				if (keys.TryGetValue (key, out existing)) {					
					continue;
				}

				keys.Add (key, value);
				resourcesStrings.Add (Tuple.Create (key, value));				
			}
		}

		return true;
	}
}