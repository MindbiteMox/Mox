using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mindbite.Mox.DirectoryListing
{
    public static class Utils
    {
        public static IEnumerable<SelectListItem> MakeSelectListTree(Type directoryType, IEnumerable<Data.DocumentDirectory> allDirectories, int addDepth = 0)
        {
            var castEnumerable = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(directoryType);

            return (IEnumerable< SelectListItem>)typeof(Utils)
                .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
                .First(x => x.Name == nameof(MakeSelectListTree) && x.GetParameters().Count() == 2)
                .MakeGenericMethod(directoryType)
                .Invoke(null, new object[] { castEnumerable.Invoke(null, new object[] { allDirectories }), addDepth })!;
        }

        public static IEnumerable<SelectListItem> MakeSelectListTree<TDirectory>(IEnumerable<TDirectory> allDirectories, int addDepth = 0) where TDirectory : Data.DocumentDirectory<TDirectory>
        {
            void AddChildren(List<(Data.DocumentDirectory directory, int depth)> result, Data.DocumentDirectory parent, int depth)
            {
                foreach (var child in allDirectories.Where(x => x.ParentDirectoryId == parent.Id))
                {
                    result.Add((child, depth));
                    AddChildren(result, child, depth + 1);
                }
            }

            var theResult = new List<(Data.DocumentDirectory directory, int depth)>();

            foreach (var rootDirectory in allDirectories.Where(x => x.ParentDirectoryId == null))
            {
                theResult.Add((rootDirectory, addDepth));
                AddChildren(theResult, rootDirectory, addDepth + 1);
            }

            return theResult.Select(x => new SelectListItem(new string('\xA0', x.depth * 4) + x.directory.Name, x.directory.Id.ToString()));
        }

        public static IEnumerable<Data.DocumentDirectory> GetParents(Type directoryType, IEnumerable<Data.DocumentDirectory> allDirectories, Data.DocumentDirectory directory)
        {
            var castEnumerable = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(directoryType);

            return ((IEnumerable)typeof(Utils)
                .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
                .First(x => x.Name == nameof(GetParents) && x.GetParameters().Count() == 2)
                .MakeGenericMethod(directoryType)
                .Invoke(null, new object[] { castEnumerable.Invoke(null, new object[] { allDirectories }), directory })!)
                .Cast<Data.DocumentDirectory>();
        }

        public static IEnumerable<TDirectory> GetParents<TDirectory>(IEnumerable<TDirectory> allDirectories, TDirectory directory) where TDirectory : Data.DocumentDirectory<TDirectory>
        {
            if (directory.ParentDirectoryId == null)
            {
                return Enumerable.Empty<TDirectory>().Append(directory);
            }
            else
            {
                var parentDirectory = allDirectories.First(x => x.Id == directory.ParentDirectoryId);
                return GetParents(allDirectories, parentDirectory).Append(directory);
            }
        }

        public static string RemoveInvalidFileNameChars(string filename)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars().Concat(System.IO.Path.GetInvalidPathChars()).Concat(new char[] { '#' }).ToArray();
            return string.Join("_", filename.Split(invalidChars));
        }

        public static List<(Guid uid, string name)> GetDirectoryPath<TDirectory>(List<TDirectory> allDirectories, TDirectory directory, bool? includeSelf = false) where TDirectory : Data.DocumentDirectory<TDirectory>
        {
            List<int> GetParentIdRec(List<int> list, TDirectory dir)
            {
                if (dir.ParentDirectoryId == null)
                {
                    return list;
                }
                var parentDirectory = allDirectories.FirstOrDefault(x => x.Id == dir.ParentDirectoryId.Value);
                if (dir.ParentDirectory == null)
                {
                    return list;
                }
                else
                {
                    list.Add(dir.ParentDirectoryId.Value);
                    return GetParentIdRec(list, parentDirectory);
                }
            }

            var breadCrumbs = GetParentIdRec(new List<int>(), directory).Select(x =>
            {
                var directory = allDirectories.First(y => y.Id == x);
                return (directory.UID, directory.Name);
            }).Reverse().ToList();

            if (includeSelf == true)
            {
                breadCrumbs.Add((directory.UID, directory.Name));
            }

            return breadCrumbs;
        }
    }
}
