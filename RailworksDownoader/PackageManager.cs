﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RailworksDownloader
{
    public class Package
    {
        public int PackageId { get; set; }
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public int Category { get; set; }
        public int Era { get; set; }
        public int Country { get; set; }
        public int Version { get; set; }
        public int Owner { get; set; }
        public DateTime Datetime { get; set; }
        public string Description { get; set; }
        public string TargetPath { get; set; }
        public bool IsPaid { get; set; }
        public int SteamAppID { get; set; }
        public List<string> FilesContained { get; set; }
        public List<int> Dependencies { get; set; }

        public Package(int package_id, string display_name, int category, int era, int country, int owner, string date_time, string target_path, List<string> deps_contained, string file_name = "", string description = "", int version = 1)
        {
            PackageId = package_id;
            FileName = file_name;
            DisplayName = display_name;
            Category = category;
            Era = era;
            Country = country;
            Version = version;
            Owner = owner;
            Datetime = Convert.ToDateTime(date_time);
            Description = description;
            TargetPath = target_path;
            IsPaid = false;
            SteamAppID = -1;
            FilesContained = deps_contained;
            Dependencies = new List<int>();
        }

        public Package(QueryContent packageJson)
        {
            PackageId = packageJson.id;
            FileName = packageJson.file_name;
            DisplayName = packageJson.display_name;
            Category = packageJson.category;
            Era = packageJson.era;
            Country = packageJson.country;
            Version = packageJson.version;
            Owner = packageJson.owner;
            Datetime = Convert.ToDateTime(packageJson.created);
            Description = packageJson.description;
            TargetPath = packageJson.target_path;
            IsPaid = packageJson.paid;
            SteamAppID = packageJson.steamappid;
            FilesContained = new List<string>();
            if (packageJson.files != null)
                FilesContained = packageJson.files.ToList();
            Dependencies = new List<int>();
            if (packageJson.dependencies != null)
                Dependencies = packageJson.dependencies.ToList();
        }
    }

    public class PackageManager
    {
        private readonly SqLiteAdapter SqLiteAdapter = new SqLiteAdapter(Path.GetFullPath("packages.mcf"));

        public List<Package> InstalledPackages { get; set; }

        public HashSet<Package> CachedPackages = new HashSet<Package>();

        private readonly object CachedLock = new object();

        private string RWPath { get; set; }

        private string AssetsPath { get; set; }

        private static Uri ApiUrl { get; set; }

        private static WebWrapper WebWrapper { get; set; }

        public PackageManager(string rwPath, Uri apiUrl)
        {
            RWPath = rwPath;
            ApiUrl = apiUrl;
            AssetsPath = Path.Combine(RWPath, "Assets");

            //string commonpath = GetFolderPath(SpecialFolder.CommonApplicationData);
            //SqLiteAdapter = new SqLiteAdapter(Path.Combine(commonpath, "DLS", "packages.mcf"));

            InstalledPackages = SqLiteAdapter.LoadInstalledPackages();
            WebWrapper = new WebWrapper(ApiUrl);
        }

        public async Task<int> FindFile(string file_name)
        {
            Package package = CachedPackages.Where(x => x.FilesContained.Contains(file_name)).First();
            if (package != null)
                return package.PackageId;

            package = CachedPackages.Where(x => x.FilesContained.Contains(file_name)).First();
            if (package != null)
                return package.PackageId;

            Package onlinePackage = await WebWrapper.SearchForFile(file_name);
            if (onlinePackage != null && onlinePackage.PackageId > 0)
            {
                lock (CachedLock)
                {
                    CachedPackages.Add(onlinePackage);
                }
                /*lock (DownloadableLock)
                    DownloadableDependencies.UnionWith(onlinePackage.DepsContained);*/
                return onlinePackage.PackageId;
            }

            return -1;
        }

        public async Task<HashSet<string>> GetDownloadableDependencies(HashSet<string> globalDependencies)
        {
            return (await WebWrapper.GetAllFiles()).Intersect(globalDependencies).ToHashSet();
        }

        public async Task<HashSet<string>> GetPaidDependencies(HashSet<string> globalDependencies)
        {
            return (await WebWrapper.GetPaidFiles()).Intersect(globalDependencies).ToHashSet();
        }
    }

}
