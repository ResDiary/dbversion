﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Data;
using DatabaseVersion.Tasks;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Criterion;

namespace DatabaseVersion.Version.NumericVersion
{
    //[Export(typeof(IVersionProvider))]
    public class NumericVersionProvider : IVersionProvider
    {
        public VersionBase CreateVersion(string versionString)
        {
            return new NumericVersion(int.Parse(versionString));
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
                return session.QueryOver<NumericVersion>()
                    .OrderBy(v => v.Version).Desc()
                    .List()
                    .FirstOrDefault();
            }
        }

        public void CreateVersionTable(System.Data.IDbConnection connection)
        {
            Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connection.ConnectionString))
                .Mappings(v => v.FluentMappings.Add<NumericVersionMap>())
                .Mappings(v=>v.FluentMappings.Add<NumericVersionTaskMap>())
                .ExposeConfiguration(c => new SchemaExport(c).Create(false, true))
                .BuildSessionFactory();
        }

        public void InsertVersion(VersionBase version, System.Data.IDbConnection connection)
        {
            var sessionFactory = this.CreateSessionFactory(connection.ConnectionString);
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                NumericVersion numericVersion = version as NumericVersion;
                if (!numericVersion.CreatedOn.HasValue)
                    numericVersion.CreatedOn = DateTime.UtcNow;
                numericVersion.UpdatedOn = DateTime.UtcNow;
                session.SaveOrUpdate(version);
                transaction.Commit();
            }
        }

        public IComparer<object> GetComparer()
        {
            return new NumericVersionComparer();
        }

        private ISessionFactory CreateSessionFactory(string connectionString)
        {
            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString))
                .Mappings(m => m.FluentMappings.Add<NumericVersionMap>())
                .Mappings(m => m.FluentMappings.Add<NumericVersionTaskMap>())
                .BuildSessionFactory();
        }

        public bool HasExecutedScript(VersionBase currentVersion, VersionBase targetVersion, IDatabaseTask task)
        {
            if (currentVersion != null)
            {
                NumericVersion currentNumericVersion = currentVersion as NumericVersion;
                NumericVersion targetNumericVersion = targetVersion as NumericVersion;
                if (currentNumericVersion.Version.Equals(targetNumericVersion.Version))
                {
                    return currentNumericVersion.HasExecutedTask(task);
                }
            }

            return false;
        }
        
        private class NumericVersionComparer : Comparer<object>
        {
            public override int Compare(object x, object y)
            {
                NumericVersion left = x as NumericVersion;
                NumericVersion right = y as NumericVersion;

                return left.Version - right.Version;
            }
        }
    }
}