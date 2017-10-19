using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace azpublishdir
{
	public class AzureBlobStorage
	{
		CloudStorageAccount storageAccount;
		CloudBlobClient blobClient;

		public string ConnectionString;
		public string Container;

		CloudStorageAccount StorageAccount {
			get {
				if (storageAccount == null)
					storageAccount = CloudStorageAccount.Parse (ConnectionString);
				return storageAccount;
			}
		}

		CloudBlobClient CloudBlobClient {
			get {
				if (blobClient == null)
					blobClient = StorageAccount.CreateCloudBlobClient ();

				return blobClient;
			}
		}

		protected async Task<CloudBlobDirectory> GetCloudBlobDirectoryAsync (string folder)
		{
			CloudBlobClient client = CloudBlobClient;
			CloudBlobContainer container = client.GetContainerReference (Container);
			await container.CreateIfNotExistsAsync ();
			return container.GetDirectoryReference (folder);
		}
	}
}
