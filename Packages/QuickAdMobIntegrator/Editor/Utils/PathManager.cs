
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickAdMobIntegrator.Editor
{
    internal sealed class PathManager
    {
        internal IReadOnlyList<string> Paths { get; private set; }
        internal IReadOnlyCollection<string> ExcludePaths => _excludePaths;
        internal bool AreAllPathsDeleted { get; private set; }

        readonly HashSet<string> _excludePaths;

        internal PathManager(IEnumerable<string> paths, IEnumerable<string> pathsToExclude = null)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            Paths = paths.Select(NormalizePath).ToList().AsReadOnly();
            var excludes = pathsToExclude?.Select(NormalizePath) ?? Enumerable.Empty<string>();
            _excludePaths = new HashSet<string>(excludes, StringComparer.OrdinalIgnoreCase);
            AreAllPathsDeleted = CheckIfAllPathsDeleted();
        }

        internal bool CheckIfAllPathsDeleted()
            => Paths.All(path => IsPathExcluded(path) || IsPathDeleted(path));

        internal Dictionary<string, Exception> DeleteAllPaths()
        {
            var result = new Dictionary<string, Exception>();

            foreach (var path in Paths)
            {
                if (IsPathExcluded(path))
                {
                    continue;
                }

                try
                {
                    if (File.Exists(path))
                    {
                        DeleteFile(path);
                        result[NormalizePath(path)] = null;
                    }
                    else if (Directory.Exists(path))
                    {
                        DeleteDirectoryContents(path, result);
                        if (!Directory.EnumerateFileSystemEntries(path).Any())
                        {
                            DeleteDirectory(path);
                            result[NormalizePath(path)] = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result[NormalizePath(path)] = ex;
                }
            }

            AreAllPathsDeleted = CheckIfAllPathsDeleted();
            
            return result;
        }

        string NormalizePath(string path)
            => Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        bool IsPathExcluded(string path)
        {
            var normalizedPath = NormalizePath(path);
            return _excludePaths.Any(excludePath =>
                normalizedPath.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase));
        }

        bool IsPathDeleted(string path)
        {
            if (File.Exists(path)) return false;

            if (Directory.Exists(path))
            {
                return Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).All(IsPathExcluded);
            }
            return true;
        }

        void DeleteDirectoryContents(string directoryPath, Dictionary<string, Exception> result)
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(directoryPath))
            {
                if (IsPathExcluded(path))
                {
                    continue;
                }

                try
                {
                    if (File.Exists(path))
                    {
                        DeleteFile(path);
                        result[NormalizePath(path)] = null;
                    }
                    else if (Directory.Exists(path))
                    {
                        DeleteDirectoryContents(path, result);
                        if (!Directory.EnumerateFileSystemEntries(path).Any())
                        {
                            DeleteDirectory(path);
                            result[NormalizePath(path)] = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result[NormalizePath(path)] = ex;
                }
            }
        }

        void DeleteFile(string path)
        {
            var normalizedPath = NormalizePath(path);
            var metaPath = normalizedPath + ".meta";
            File.Delete(path);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        void DeleteDirectory(string path)
        {
            var normalizedPath = NormalizePath(path);
            var metaPath = normalizedPath + ".meta";
            Directory.Delete(path);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }
}