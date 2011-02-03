﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using Ninject.Modules;
using JollyBit.BS.Server.Networking;
using JollyBit.BS.Core.Utility;
using JsonExSerializer;
using System.IO;
using JollyBit.BS.Core.World;

namespace JollyBit.BS.Server
{
    public class BSServerNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Kernel.Load(new JollyBit.BS.Core.BSCoreNinjectModule());
            Rebind<IConnectionManager>().To<ConnectionManager>().InSingletonScope();
            Rebind<IBlockManager>().To<JollyBit.BS.Server.World.BlockManager>().InSingletonScope();
            Rebind<IConfigManager>().ToMethod(
                (context) =>
                {
                    //XmlSerializer
                    Stream stream = context.Kernel.Get<IFileSystem>().OpenFile("ServerConfig.json");
                    ConfigManager configManager;
                    if (stream != null)
                    {
                        TextReader reader = new StreamReader(stream);
                        JsonExSerializer.Serializer serializer = new JsonExSerializer.Serializer(typeof(ConfigManager));
                        configManager = serializer.Deserialize(stream) as ConfigManager;
                        stream.Close();
                    }
                    else configManager = new ConfigManager();
                    return configManager;
                }).InSingletonScope();
        }
    }
}
