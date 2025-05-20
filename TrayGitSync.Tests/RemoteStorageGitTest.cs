using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using TrayGitSync.Exceptions;

namespace TrayGitSync.Tests;

[TestClass]
public class RemoteStorageGitTest
{
    private readonly string _machineName = Environment.MachineName.ToUpper().Trim();

    private const string SyncTestPath = "C:\\sync-test";
    [TestMethod]
    public void Upload_RepositoryExists()
    {
        var remoteStorage = new RemoteStorageGit();
        Assert.IsTrue(Directory.Exists(SyncTestPath), "C:\\sync-test directory does not exist, create and initialize it");
        Assert.IsTrue(Directory.Exists(Path.Join(SyncTestPath, ".git")), "C:\\sync-test is not a git repository");
        
        var currentDate = DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss");
        File.WriteAllText(Path.Combine(SyncTestPath, $"{currentDate}.txt"), string.Empty);

        var config = new Configuration
        {
            Repositories =
            [
                new Repository
                {
                    Name = "SyncTest",
                    RemoteUrl = "git@github.com:trittimo/sync-test.git",
                    MachinePaths = new Dictionary<string, string>
                    {
                        { _machineName, SyncTestPath }
                    }
                }
            ]
        };
        remoteStorage.Upload(config);
    }
    
    [TestMethod]
    public void Download_RepositoryExists()
    {
        var remoteStorage = new RemoteStorageGit();
        Assert.IsTrue(Directory.Exists(SyncTestPath), "C:\\sync-test directory does not exist, create and initialize it");
        Assert.IsTrue(Directory.Exists(Path.Join(SyncTestPath, ".git")), "C:\\sync-test is not a git repository");

        var config = new Configuration
        {
            Repositories =
            [
                new Repository
                {
                    Name = "SyncTest",
                    RemoteUrl = "git@github.com:trittimo/sync-test.git",
                    MachinePaths = new Dictionary<string, string>
                    {
                        { _machineName, SyncTestPath }
                    }
                }
            ]
        };
        remoteStorage.Download(config);
    }

    [TestMethod]
    [ExpectedException(typeof(RepositoryPathNotFoundException))]
    public void Upload_InvalidMachinePath_ThrowsException()
    {
        var remoteStorage = new RemoteStorageGit();
        var config = new Configuration
        {
            Repositories =
            [
                new Repository
                {
                    Name = "SyncTest",
                    RemoteUrl = "git@github.com:trittimo/sync-test.git",
                    MachinePaths = new Dictionary<string, string>
                    {
                        { "INVALID_MACHINE_NAME", SyncTestPath }
                    }
                }
            ]
        };

        remoteStorage.Upload(config);
    }
}