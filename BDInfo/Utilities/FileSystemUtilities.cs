using System.IO;
using DiscUtils;
using DiscUtils.Udf;

namespace BDInfo.Utilities
{
   public static class FileSystemUtilities
    {
        public static DiscFileSystem GetFileSystem(string path, ref Stream isoStream)
        {
            DiscFileSystem result;

            if (File.Exists(path))
            {
                isoStream = File.Open(path, FileMode.Open);
                result = new UdfReader(isoStream);
            }
            else
            {
                result = new NativeFileSystem(path, true);
            }

            return result;
        }
    }
}
