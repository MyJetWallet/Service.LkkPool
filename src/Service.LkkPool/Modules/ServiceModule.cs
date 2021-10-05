using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Sdk.NoSql;
using Service.LkkPool.NoSql;
using Service.LkkPool.Services;

namespace Service.LkkPool.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterMyNoSqlWriter<PoolConfigNoSql>(() => Program.Settings.NoSqlWriterUrl, PoolConfigNoSql.TableName);
            builder.RegisterMyNoSqlWriter<LevelModelNoSql>(() => Program.Settings.NoSqlWriterUrl,
                LevelModelNoSql.TableName);
                
            builder.RegisterType<PoolManager>().SingleInstance().AutoActivate().AsSelf();
        }
    }
}