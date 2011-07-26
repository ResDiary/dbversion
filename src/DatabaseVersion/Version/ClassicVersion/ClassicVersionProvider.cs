﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Data;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Criterion;

namespace DatabaseVersion.Version.ClassicVersion
{
    [Export(typeof(IVersionProvider))]
    public class ClasicVersionProvider : IVersionProvider
    {
        public VersionBase CreateVersion(string versionString)
        {
            return new ClassicVersion(versionString);
        }

        public bool VersionTableExists(System.Data.IDbConnection connection)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "select count(1) from information_schema.tables where table_name = 'Version'";
                var count = (int)command.ExecuteScalar();

                return count == 1;
            }
        }

        public VersionBase GetCurrentVersion(System.Data.IDbConnection connection)
        {
            ISessionFactory sessionFactory = this.CreateSessionFactory(connection.ConnectionString);
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                return session.QueryOver<ClassicVersion>()
                    .OrderBy(v => v.Version).Desc()
                    .List()
                    .FirstOrDefault();
            }
        }

        public void CreateVersionTable(System.Data.IDbConnection connection)
        {
            Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connection.ConnectionString))
                .Mappings(v => v.FluentMappings.Add<ClassicVersionMap>())
                .Mappings(v => v.FluentMappings.Add<ClassicScriptMap>())
                .ExposeConfiguration(c => new SchemaExport(c).Create(false, true))
                .BuildSessionFactory();
        }

        public void InsertVersion(VersionBase version, System.Data.IDbConnection connection)
        {
            var sessionFactory = this.CreateSessionFactory(connection.ConnectionString);
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                ClassicVersion classicVersion = version as ClassicVersion;
                if (!classicVersion.CreatedOn.HasValue)
                    classicVersion.CreatedOn = DateTime.UtcNow;
                classicVersion.UpdatedOn = DateTime.UtcNow;
                session.SaveOrUpdate(version);

                transaction.Commit();
            }
        }

        public IComparer<object> GetComparer()
        {
            return new ClassicVersionComparer();
        }

        private ISessionFactory CreateSessionFactory(string connectionString)
        {
            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString))
                .Mappings(m => m.FluentMappings.Add<ClassicVersionMap>())
                .Mappings(m => m.FluentMappings.Add<ClassicScriptMap>())
                .BuildSessionFactory();
        }

        public bool HasExecutedScript(VersionBase currentVersion, VersionBase targetVersion, string scriptName)
        {
            if (currentVersion != null)
            {
                ClassicVersion currentClassicVersion = currentVersion as ClassicVersion;
                ClassicVersion targetClassicVersion = targetVersion as ClassicVersion;
                if (currentClassicVersion.SystemVersion.Equals(targetClassicVersion.SystemVersion))
                {
                    if (currentClassicVersion.Scripts.Contains(new ClassicVersionScript(currentClassicVersion, scriptName)))
                        return true;
                }
            }

            return false;
        }

        public IEqualityComparer<Script> GetScriptComparer()
        {
            return new ClassicVersionScriptComparer();
        }

        private class ClassicVersionComparer : Comparer<object>
        {
            public override int Compare(object x, object y)
            {
                ClassicVersion left = x as ClassicVersion;
                ClassicVersion right = y as ClassicVersion;

                return left.Version.CompareTo(right.Version);
            }
        }

        private class ClassicVersionScriptComparer : IEqualityComparer<Script>
        {
            public bool Equals(Script x, Script y)
            {
                ClassicVersionScript left = x as ClassicVersionScript;
                ClassicVersionScript right = y as ClassicVersionScript;

                if ((x == null) || (y == null))
                    return false;

                return (left.Version.Equals(right.Version) &&
                        left.Name.Equals(right.Name, StringComparison.InvariantCultureIgnoreCase));
            }

            public int GetHashCode(Script obj)
            {
                ClassicVersionScript script = obj as ClassicVersionScript;
                return script.Id.GetHashCode();
            }
        }
    }
}
