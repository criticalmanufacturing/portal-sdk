using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class Utilities
    {
        /// <summary>
        /// Gets object by Id
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="name">The object name</param>
        /// <param name="levelsToLoad">Levels to Load</param>
        /// <returns></returns>
        public static T GetObjectByName<T>(String name, Int32 levelsToLoad = 0) where T : CoreBase, new()
        {
            return new GetObjectByNameInput
            {
                Name = name,
                Type = typeof(T).BaseType.Name == typeof(CoreBase).Name ? new T() : (object)new T().GetType().Name,
                LevelsToLoad = levelsToLoad
            }.GetObjectByNameSync().Instance as T;
        }
    }
}
