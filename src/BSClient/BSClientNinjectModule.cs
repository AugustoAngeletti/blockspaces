﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject.Modules;
using Ninject;
using JollyBit.BS.Utility;
using Ninject.Parameters;
using System.IO;
using System.Windows.Forms;

namespace JollyBit.BS
{
    public class BSClientNinjectModule : NinjectModule
    {
        public override void Load()
        {
            string path = Application.ExecutablePath.Substring(0, Application.ExecutablePath.Length - Path.GetFileName(Application.ExecutablePath).Length);
            Bind<IFileSystem>().To<StandardFileSystem>().InSingletonScope()
                .WithConstructorArgument("workingDirectory", path + "assets/");
        }
    }
}
