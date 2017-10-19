using System;

using Mono.Options;

namespace azpublishdir
{
	public class Samples
	{
		public static int Main (string [] args)
		{
			try {
				var show_help = false;
				var uploader = new AzureDirectoryUploader ();

				var os = new OptionSet {
					{ "h|help|?", (v) => show_help = true },
					{ "connection-string=", (v) => uploader.ConnectionString = v },
					{ "input-directory=", (v) => uploader.InputDirectory = v },
					{ "container=", (v) => uploader.Container = v },
					{ "target-directory=", (v) => uploader.TargetFolder = v },
				};

				var unprocessed = os.Parse (args);
				if (unprocessed.Count > 0) {
					foreach (var arg in unprocessed)
						Console.WriteLine ("Unknown command-line option: {0}", arg);
					return 1;
				}

				if (show_help) {
					Console.WriteLine ("azpublishdir [options]");
					os.WriteOptionDescriptions (Console.Out);
					return 0;
				}

				if (string.IsNullOrEmpty (uploader.ConnectionString)) {
					Console.WriteLine ("--connection-string=<string> is required");
					return 1;
				}

				if (string.IsNullOrEmpty (uploader.InputDirectory)) {
					Console.WriteLine ("--input-directory=<string> is required");
					return 1;
				}

				if (string.IsNullOrEmpty (uploader.Container)) {
					Console.WriteLine ("--container=<string> is required");
					return 1;
				}

				if (string.IsNullOrEmpty (uploader.TargetFolder)) {
					Console.WriteLine ("--target-folder=<string> is required");
					return 1;
				}

				uploader.Upload ().Wait ();
				return 0;
			} catch (Exception e) {
				Console.WriteLine (e);
				return 1;
			}
		}
	}
}
