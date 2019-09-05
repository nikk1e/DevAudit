using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sprache;
using Versatile;

namespace DevAudit.AuditLibrary
{
    public class LibmanPackageSource : PackageSource, IDeveloperPackageSource
    {
/**
 * 
 *  NICK: WARNING, This is a bit cheeky, but we're using the npm package source to check libman json files until DevAudit supports it
 * 
 * 
 */
        #region Constructors
        public LibmanPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
        }        
        #endregion

        #region Overriden properties
        public override string PackageManagerId { get { return "npm"; } } //OSS Index doesnt have a libman discriminator, assume npm has what we want

        public override string PackageManagerLabel { get { return "Libman"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return "libman.json"; } }
        #endregion

        #region Overriden methods
        //Get bower packages from reading bower.json
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            var packages = new List<Package>();

            AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
            JObject json = (JObject)JToken.Parse(config_file.ReadAsText());

            JArray libraries = (JArray)json["libraries"];

            foreach(var d in libraries)
            {
                var library = d["library"].ToString().Split('@');
                var name = library[0].ToLower();
                var version = library[1];

                packages.AddRange(GetDeveloperPackages(name, version));

                if (name.EndsWith(".js"))
                {
                    packages.AddRange(GetDeveloperPackages(name.Replace(".js", ""), version));
                }
            }

            return packages;           
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = SemanticVersion.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }
        #endregion

        #region Properties
        public string DefaultPackageSourceLockFile {get; } = "";

        public string PackageSourceLockFile {get; set;}
        #endregion

        #region Methods
        public bool PackageVersionIsRange(string version)
        {
            var lcs = SemanticVersion.Grammar.Range.Parse(version);
            if (lcs.Count > 1) 
            {
                return true;
            }
            else if (lcs.Count == 1)
            {
                var cs = lcs.Single();
                if (cs.Count == 1 && cs.Single().Operator == ExpressionType.Equal)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else throw new ArgumentException($"Failed to parser {version} as a version.");
        }

        public List<string> GetMinimumPackageVersions(string version)
        {
            var minVersions = new List<string>();
            try
            {
                var lcs = SemanticVersion.Grammar.Range.Parse(version);
                foreach (ComparatorSet<SemanticVersion> cs in lcs)
                {
                    if (cs.Count == 1 && cs.Single().Operator == ExpressionType.Equal)
                    {
                        minVersions.Add(cs.Single().Version.ToNormalizedString());
                    }
                    else
                    {
                        var gt = cs.Single(c => c.Operator == ExpressionType.GreaterThan || c.Operator == ExpressionType.GreaterThanOrEqual);
                        if (gt.Operator == ExpressionType.GreaterThan)
                        {
                            var v = gt.Version;
                            minVersions.Add((v++).ToNormalizedString());
                            this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.",
                                this.PackageManagerLabel, (v++).ToNormalizedString(), version);
                        }
                        else
                        {
                            minVersions.Add(gt.Version.ToNormalizedString());
                            this.AuditEnvironment.Info("Using {0} package version {1} which satisfies range {2}.",
                                this.PackageManagerLabel, gt.Version.ToNormalizedString(), version);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                minVersions.Add("*");
            }

            return minVersions;
        }

        public List<Package> GetDeveloperPackages(string name, string version, string vendor = null, string group = null, string architecture = null)  
        {
            return GetMinimumPackageVersions(version).Select(v => new Package(PackageManagerId, name, v, vendor, group, architecture)).ToList();
        }
        #endregion
    }
}
