using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage;

using Newtonsoft.Json.Linq;

namespace azpublishdir
{
	public class AzureDirectoryUploader : AzureBlobStorage
	{
		public string InputDirectory;
		public string TargetFolder;

		public async Task Upload ()
		{
			CloudBlobDirectory destDir = await GetCloudBlobDirectoryAsync (TargetFolder);

			UploadDirectoryOptions options = new UploadDirectoryOptions {
				SearchPattern = "*",
				Recursive = true,
				BlobType = BlobType.BlockBlob,
			};

			using (MemoryStream journalStream = new MemoryStream ()) {
				// Store the transfer context in a streamed journal.
				DirectoryTransferContext context = new DirectoryTransferContext (journalStream);

				// Register for transfer event.
				context.FileTransferred += FileTransferredCallback;
				context.FileFailed += FileFailedCallback;
				context.FileSkipped += FileSkippedCallback;

				context.SetAttributesCallback = (destination) => {
					CloudBlob destBlob = destination as CloudBlob;
					switch (Path.GetExtension (destBlob.Name).ToLowerInvariant ()) {
					case "html":
					case ".html":
						destBlob.Properties.ContentType = "text/html";
						break;
					default:
						destBlob.Properties.ContentType = "text/plain";
						break;
					}
					Console.WriteLine ($"Setting attributes for {destination}");
				};

				context.ShouldTransferCallback = (source, destination) => {
					// Can add more logic here to evaluate whether really need to transfer the target.
					Console.WriteLine ($"Should transfer from {source} to {destination}? YES");
					return true;
				};

				// Create CancellationTokenSource used to cancel the transfer
				CancellationTokenSource cancellationSource = new CancellationTokenSource ();

				TransferStatus transferStatus = null;

				StartCancelThread (cancellationSource);

				try {
					// Start the upload
					Task<TransferStatus> task = TransferManager.UploadDirectoryAsync (InputDirectory, destDir, options, context, cancellationSource.Token);
					transferStatus = await task;
				} catch (Exception e) {
					Console.WriteLine ("The transfer failed: {0}", e.Message);
				}

				Console.WriteLine ("Final transfer state: {0}", TransferStatusToString (transferStatus));
				Console.WriteLine ("Files in directory {0} uploading to {1} is finished.", InputDirectory, destDir.Uri);
			}
		}

		[DllImport ("/usr/lib/libc.dylib")]
		extern static int isatty (int filedes);

		static void StartCancelThread (CancellationTokenSource source)
		{
			if (isatty (2) == 0)
				return; // we don't have a controlling terminal, so nobody can hit enter.

			var cancel_thread = new Thread (() => {
				Console.WriteLine ("Press ENTER to cancel upload...");
				Console.ReadLine ();
				source.Cancel ();
			}) {
				IsBackground = true,
			};
			cancel_thread.Start ();
		}

		static void FileTransferredCallback (object sender, TransferEventArgs e)
		{
			Console.WriteLine ("Transfer Succeeds. {0} -> {1}.", e.Source, e.Destination);
		}

		static void FileFailedCallback (object sender, TransferEventArgs e)
		{
			Console.WriteLine ("Transfer fails. {0} -> {1}. Error message: {2}", e.Source, e.Destination, e.Exception.Message);
		}

		static void FileSkippedCallback (object sender, TransferEventArgs e)
		{
			Console.WriteLine ("Transfer skips. {0} -> {1}.", e.Source, e.Destination);
		}

		/// <summary>
		/// Format the TransferStatus of DMlib to printable string 
		/// </summary>
		public static string TransferStatusToString (TransferStatus status)
		{
			return string.Format ("Transferred bytes: {0}; Transfered: {1}; Skipped: {2}, Failed: {3}",
				status?.BytesTransferred,
				status?.NumberOfFilesTransferred,
				status?.NumberOfFilesSkipped,
				status?.NumberOfFilesFailed);
		}
	}
}
