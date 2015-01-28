﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Vbe.Interop;
using Rubberduck.Extensions;

namespace Rubberduck.SourceControl
{
    //todo: I need a way to get to the branch status...
    class GitProvider : SourceControlProviderBase
    {
        private LibGit2Sharp.Repository repo;

        public GitProvider(VBProject project, Repository repository)
            : base(project, repository)
        {
            repo = new LibGit2Sharp.Repository(CurrentRepository.LocalLocation);
        }

        ~GitProvider()
        {
            repo.Dispose();
        }

        public override string CurrentBranch
        {
            get
            {
                return repo.Branches.Where(b => !b.IsRemote && b.IsCurrentRepositoryHead).First().Name;
            }
        }

        public override IEnumerable<string> Branches
        {
            get
            {
                return repo.Branches.Where(b => !b.IsRemote)
                                    .Select(b => b.Name);
            }
        }

        //note: should we really have a clone method? I don't think we'll use it, but could be useful in an API.
        public override Repository Clone(string remotePathOrUrl, string workingDirectory)
        {
            //todo: parse name from remote path
            var name = String.Empty;
            return new Repository(name, workingDirectory, remotePathOrUrl);
        }

        public override Repository Init(string directory)
        {
            //todo: implement
            var repository = base.Init(directory);

            throw new NotImplementedException();

            return repository;
        }

        public override void Push()
        {
            //todo: put this someplace safe and preferably encrypted
            var creds = new UsernamePasswordCredentials()
            {
                Username = "ckuhn203",
                Password = "Macc2232"
            };
            
            LibGit2Sharp.Handlers.CredentialsHandler credHandler = (url, user, cred) => creds;

            var options = new PushOptions()
            {
                CredentialsProvider = credHandler
            };

            var branch = repo.Branches[this.CurrentBranch];
            repo.Network.Push(branch, options);
        }

        public override void Fetch()
        {
            //todo: break dependency on origin remote
            //todo: document the fact that git integration only works on remotes named "origin"

            try
            {
                var remote = repo.Network.Remotes["origin"];
                repo.Network.Fetch(remote);
            }
            catch (LibGit2SharpException ex)
            {
                //todo: all actions will require this pattern; 
                throw new SourceControlException(ex.Message, ex);
            }

        }

        public override void Pull()
        {
            var options = new PullOptions()
            {
                MergeOptions = new MergeOptions()
                {
                    FastForwardStrategy = FastForwardStrategy.Default
                }
            };

            var signature = GetSignature();
            repo.Network.Pull(signature, options);

            base.Pull();
        }

        public override void Commit(string message)
        {
            base.Commit(message);

            RepositoryStatus status = repo.RetrieveStatus();
            List<string> filePaths = status.Modified.Select(mods => mods.FilePath).ToList();
            repo.Stage(filePaths);
            repo.Commit(message);
        }

        public override void Merge(string sourceBranch, string destinationBranch)
        {
            repo.Checkout(repo.Branches[destinationBranch]);

            Signature signature = GetSignature();
            repo.Merge(repo.Branches[sourceBranch], signature);
        }

        public override void Checkout(string branch)
        {
            repo.Checkout(repo.Branches[branch]);

            base.Checkout(branch);
        }

        public override void Revert()
        {
            //todo: investigate revert results class
            repo.Revert(repo.Head.Tip, GetSignature());

            base.Revert();
        }

        public override void AddFile(string filePath)
        {
            // https://github.com/libgit2/libgit2sharp/wiki/Git-add
            repo.Stage(filePath);
        }

        public override void RemoveFile(string filePath)
        {
            //todo: implement
            throw new NotImplementedException();
        }

        private Signature GetSignature()
        {
            return this.repo.Config.BuildSignature(DateTimeOffset.Now);
            //todo: clean up dead code
            // return new Signature("ckuhn203", "ckuhn203@gmail.com", DateTimeOffset.Now);
        }
    }
}
