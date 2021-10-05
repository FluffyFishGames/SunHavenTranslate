﻿using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SunHavenTranslate
{
    public static class AssetUtils
    {
        public static bool AllDependenciesLoaded(AssetsManager am, AssetsFileInstance afi)
        {
            foreach (AssetsFileDependency dep in afi.file.dependencies.dependencies)
            {
                string absAssetPath = dep.assetPath;
                if (absAssetPath.StartsWith("archive:/"))
                {
                    return false; //todo
                }
                if (!Path.IsPathRooted(absAssetPath))
                {
                    absAssetPath = Path.Combine(Path.GetDirectoryName(afi.path), dep.assetPath);
                }
                if (!am.files.Any(d => d != null && Path.GetFileName(d.path).ToLower() == Path.GetFileName(absAssetPath).ToLower()))
                {
                    return false;
                }
            }
            return true;
        }

        //used as sort of a hack to handle both second and component
        //in m_Component which both have the pptr as the last field
        //(pre 5.5 has a first and second while post 5.5 has component)
        public static AssetTypeValueField GetLastChild(this AssetTypeValueField atvf)
        {
            return atvf[atvf.childrenCount - 1];
        }
    }
}
