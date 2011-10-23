namespace dropkick.Tasks.Files
{
    using System;
    using System.IO;
    using DeploymentModel;
    using Exceptions;
    using log4net;
    using ExtensionsToString = Magnum.Extensions.ExtensionsToString;
    using Path = FileSystem.Path;
	using System.Text.RegularExpressions;
	using System.Linq;
	using System.Globalization;

    public abstract class BaseIoTask :
        BaseTask
    {
        readonly ILog _fileLog = Logging.WellKnown.FileChanges;
        protected readonly Path _path;

        protected BaseIoTask(Path path)
        {
            _path = path;
        }

        public void LogFileChange(string format, params object[] args)
        {
            _fileLog.InfoFormat(format, args);
        }

        protected void ValidateIsFile(DeploymentResult result, string path)
        {
            if (!(new FileInfo(_path.GetFullPath(path)).Exists)) result.AddAlert("'{0}' does not exist.".FormatWith(path));

            if (!_path.IsFile(path)) result.AddError("'{0}' is not a file.".FormatWith(path));
        }

        protected void ValidateIsDirectory(DeploymentResult result, string path)
        {
            if (!(new DirectoryInfo(_path.GetFullPath(path)).Exists)) result.AddAlert("'{0}' does not exist and will be created.".FormatWith(path));

            if (!_path.IsDirectory(path)) result.AddError("'{0}' is not a directory.".FormatWith(path));
        }

        protected void ValidatePath(DeploymentResult result, string path)
        {
            try
            {
                if (!_path.IsDirectory(path))
                {
                    throw new DeploymentException("'{0}' is not an acceptable path. Must be a directory".FormatWith(path));
                }
            }
            catch (Exception)
            {
                throw new DeploymentException("'{0}' is not an acceptable path. Must be a directory".FormatWith(path));
            }
        }

        protected void CopyDirectory(DeploymentResult result, DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }
            else
            {
                RemoveReadOnlyAttributes(destination, result);
            }

            // Copy all files.
			FileInfo[] files = source.GetFiles();

			foreach (FileInfo file in files)
			{
                string fileDestination = _path.Combine(destination.FullName,file.Name);
				CopyFileToFile(result, file, new FileInfo(fileDestination));
			}

            // Process subdirectories.
            DirectoryInfo[] dirs = source.GetDirectories();
            foreach (var dir in dirs)
            {
                // Get destination directory.
                string destinationDir = _path.Combine(destination.FullName, dir.Name);

                // Call CopyDirectory() recursively.
                CopyDirectory(result, dir, new DirectoryInfo(destinationDir));
            }
        }

		/// <summary>
		/// Determines whether a string matches the given ignore patterns.  This is used
		/// to ignore files which shouldn't be copied from the source to target directories,
		/// e.g. you may first place an App_Offline.htm file into the directory before copying
		/// data to it.  You wouldn't want to delete the App_Offline.html file while copying files.
		/// </summary>
		/// <param name="ignorePatterns"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private bool IsIgnored(Regex[] ignorePatterns, string fileName)
		{
			bool returnValue = false;

			foreach (Regex ignorePattern in ignorePatterns)
			{
				if (ignorePattern.IsMatch(fileName))
				{
					returnValue = true;
					break;
				}
			}

			return returnValue;
		}

		/// <summary>
		/// Clears the contents of a given directory.
		/// </summary>
		/// <param name="result">The Deployment results.</param>
		/// <param name="directory">The directory to clear.</param>
		protected void ClearDirectoryContents(DeploymentResult result, DirectoryInfo directory)
		{
			ClearDirectoryContents(result, directory, new Regex[] { });
		}

		/// <summary>
		/// Clears the contents of a given directory.
		/// </summary>
		/// <param name="result">The Deployment results.</param>
		/// <param name="directory">The directory to clear.</param>
		/// <param name="ignorePatterns">Regular expressions which match the files to ignore, e.g. "[aA]pp_[Oo]ffline\.htm".</param>
		protected void ClearDirectoryContents(DeploymentResult result, DirectoryInfo directory, Regex[] ignorePatterns)
		{
			if(ignorePatterns == null)
			{
				ignorePatterns = new Regex[] {};
			}

			result.Add(new DeploymentItem(DeploymentItemStatus.Good, string.Format(CultureInfo.InvariantCulture, "Clearing directory \"{0}\".", directory.Name)));

			// Delete files.
			FileInfo[] files = directory.GetFiles();
			foreach(FileInfo file in files.Where(f => !IsIgnored(ignorePatterns, f.Name)))
			{
				File.Delete(file.FullName);
			}

			// Delete subdirectories.
			foreach(DirectoryInfo subdirectory in directory.GetDirectories())
			{
				Directory.Delete(subdirectory.FullName, true);
			}

			result.Add(new DeploymentItem(DeploymentItemStatus.Good, string.Format(CultureInfo.InvariantCulture, "Directory \"{0}\" cleared.", directory.Name)));
		}

        protected void DeleteDestinationFirst(DirectoryInfo directory, DeploymentResult result)
        {
            if (directory.Exists)
            {
                RemoveReadOnlyAttributes(directory, result);

                directory.Delete(true);
                result.AddGood("'{0}' was successfully deleted".FormatWith(directory.FullName));
                //TODO: a delete list?
            }
        }

        private void RemoveReadOnlyAttributes(DirectoryInfo directory, DeploymentResult result)
        {
            try
            {
                directory.Attributes = (directory.Attributes & ~FileAttributes.ReadOnly);

                foreach (var subdirectory in directory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    subdirectory.Attributes = (subdirectory.Attributes & ~FileAttributes.ReadOnly);
                }

                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    RemoveReadOnlyAttributes(file, result);
                }
            }
            catch (Exception ex)
            {
                result.AddAlert("Had an issue when attempting to remove directory readonly attributes:{0}{1}", Environment.NewLine, ex.ToString());
                // LogFineGrain("[copy][attributeremoval][warning] Had an error when attempting to remove directory/file attributes:{0}{1}",Environment.NewLine,ex.ToString());
            }
        }

        private static void RemoveReadOnlyAttributes(FileInfo file, DeploymentResult result)
        {
            if (file.IsReadOnly)
            {
                file.Attributes = (file.Attributes & ~FileAttributes.ReadOnly);
                //File.SetAttributes(file.FullName, File.GetAttributes(file.FullName) & ~FileAttributes.ReadOnly);
                //destination.IsReadOnly = false;        
            }
        }

        protected void CopyFile(DeploymentResult result, string newFileName, string from, string to)
        {
            if (ExtensionsToString.IsNotEmpty(newFileName))
                CopyFileToFile(result, new FileInfo(from), new FileInfo(_path.Combine(to, newFileName)));
            else
                CopyFileToDirectory(result, new FileInfo(from), new DirectoryInfo(to));
        }

        void CopyFileToDirectory(DeploymentResult result, FileInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists) destination.Create();
            CopyFileToFile(result, source, new FileInfo(_path.Combine(destination.FullName, source.Name)));
        }

        void CopyFileToFile(DeploymentResult result, FileInfo source, FileInfo destination)
        {
            var overwrite = destination.Exists;

            if (overwrite)
            {
                RemoveReadOnlyAttributes(destination, result);
            }

            source.CopyTo(destination.FullName, true);

            string copyLogPrefix = "[copy]";
            if (overwrite)
            {
                copyLogPrefix = "[copy][overwrite]";
            }

            LogFineGrain("{0} '{1}' -> '{2}'", copyLogPrefix, source.FullName, destination.FullName);

            //TODO: Adjust for remote pathing
            _fileLog.Info(destination.FullName); //log where files are modified
        }
    }
}