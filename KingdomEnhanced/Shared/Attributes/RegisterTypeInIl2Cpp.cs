using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if IL2CPP
using Il2CppInterop.Runtime.Injection;
#endif

namespace KingdomEnhanced.Shared.Attributes
{
#if IL2CPP
    /// <summary>Attribute that marks classes to be registered with the IL2CPP interop injector.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterTypeInIl2Cpp : Attribute
    {
        /// <summary>Assemblies queued for registration until the injector is ready.</summary>
        internal static List<Assembly> registrationQueue = new();
        /// <summary>Whether the IL2CPP registration system is ready to process assemblies.</summary>
        internal static bool ready;
        /// <summary>Whether successful registrations should be logged.</summary>
        internal bool LogSuccess = true;

        /// <summary>Creates a registration attribute with default logging enabled.</summary>
        public RegisterTypeInIl2Cpp() { }

        /// <summary>Creates a registration attribute with configurable logging.</summary>
        public RegisterTypeInIl2Cpp(bool logSuccess)
        {
            LogSuccess = logSuccess;
        }

        /// <summary>Registers every class in the assembly that carries this attribute with the IL2CPP injector.</summary>
        public static void RegisterAssembly(Assembly asm)
        {
            IEnumerable<Type> types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }

            foreach (var type in types)
            {
                if (!type.IsClass)
                    continue;

                var attr = type.GetCustomAttribute<RegisterTypeInIl2Cpp>(false);
                if (attr == null)
                    continue;

                try
                {
                    ClassInjector.RegisterTypeInIl2Cpp(type);
                    if (attr.LogSuccess)
                        UnityEngine.Debug.Log($"[RegisterTypeInIl2Cpp] Registered: {type.FullName}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[RegisterTypeInIl2Cpp] Failed to register {type.FullName}: {ex}");
                }
            }
        }

        /// <summary>Marks the registration system as ready and flushes all queued assemblies.</summary>
        public static void SetReady()
        {
            ready = true;

            foreach (var asm in registrationQueue)
                RegisterAssembly(asm);

            registrationQueue.Clear();
        }

        /// <summary>Installs the assembly-load hook and registers all currently loaded assemblies.</summary>
        public static void InitRegisterHook()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
            {
                if (!ready)
                    registrationQueue.Add(args.LoadedAssembly);
                else
                    RegisterAssembly(args.LoadedAssembly);
            };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ready)
                    RegisterAssembly(asm);
                else
                    registrationQueue.Add(asm);
            }
        }
    }
#endif
}
