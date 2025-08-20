namespace RSecurityBackend.Models.Auth.Memory
{
    /// <summary>
    /// securable itmes: forms and ...
    /// </summary>
    public class SecurableItem
    {
        /// <summary>
        /// user
        /// </summary>
        public const string UserEntityShortName = "user";

        /// <summary>
        /// role
        /// </summary>
        public const string RoleEntityShortName = "role";

        /// <summary>
        /// audit
        /// </summary>
        public const string AuditLogEntityShortName = "audit";

        /// <summary>
        /// global options
        /// </summary>
        public const string GlobalOptionsEntityShortName = "option";

        /// <summary>
        /// workspaces
        /// </summary>
        public const string WorkspaceEntityShortName = "workspace";

        /// <summary>
        /// workspace role
        /// </summary>
        public const string WorkspaceRoleEntityShortName = "wsrole";

        /// <summary>
        /// notifications
        /// </summary>
        public const string NotificationEntityShortName = "notif";

        /// <summary>
        /// view
        /// </summary>
        public const string ViewOperationShortName = "view";
        /// <summary>
        /// add
        /// </summary>
        public const string AddOperationShortName = "add";
        /// <summary>
        /// modify
        /// </summary>
        public const string ModifyOperationShortName = "modify";
        /// <summary>
        /// delete
        /// </summary>
        public const string DeleteOperationShortName = "delete";
        /// <summary>
        /// sessions
        /// </summary>
        public const string SessionsOperationShortName = "sessions";
        /// <summary>
        /// delothersession
        /// </summary>
        public const string DelOtherUserSessionOperationShortName = "delothersession";
        /// <summary>
        /// view all
        /// </summary>
        public const string ViewAllOperationShortName = "viewall";
        /// <summary>
        /// administer
        /// </summary>
        public const string Administer = "administer";
        /// <summary>
        /// invite members
        /// </summary>
        public const string InviteMembersOperationShortName = "invite";
        /// <summary>
        /// remove members
        /// </summary>
        public const string RemoveMembersOperationShortName = "deluser";
        /// <summary>
        /// change member status
        /// </summary>
        public const string ChangeMemberStatusOperationShortName = "moduser";
        /// <summary>
        /// modify member role
        /// </summary>
        public const string ChangeMemberRoleShortName = "wsuserrole";
        /// <summary>
        /// view members
        /// </summary>
        public const string QueryMembersListOperationShortName = "vumembers";

        /// <summary>
        /// bulk operation
        /// </summary>
        public const string BulkOpertaionShortName = "bulk";



        /// <summary>
        /// list of forms and their permissions
        /// </summary>
        public static SecurableItem[] Items
        {
            get
            {
                return
                [
                    new SecurableItem()
                    {
                        ShortName = UserEntityShortName,
                        Description = "کاربران",
                        Operations =
                        [
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false, null ),
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false,
                            [
                                new SecurableItemOperationPrerequisite(  RoleEntityShortName, ViewOperationShortName)
                            ]
                           ),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false,
                            [
                                new SecurableItemOperationPrerequisite(  RoleEntityShortName, ViewOperationShortName)
                            ]
                            ),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                            new SecurableItemOperation(SessionsOperationShortName, "مشاهده جلسات همه کاربران", false, null),
                            new SecurableItemOperation(DelOtherUserSessionOperationShortName, "حذف جلسه سایر کاربران", false),
                            new SecurableItemOperation(ViewAllOperationShortName, "مشاهده اطلاعات کاربران دیگر", false, null ),
                            new SecurableItemOperation(Administer, "مدیریت کاربران", false, null ),
                        ]
                    },
                    new SecurableItem()
                    {
                        ShortName = RoleEntityShortName,
                        Description = "نقش‌ها",
                        Operations =
                        [
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false),
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                        ]
                    },
                    new SecurableItem()
                    {
                        ShortName = AuditLogEntityShortName,
                        Description = "رویدادها",
                        Operations =
                        [
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false),
                        ]
                    },
                    new SecurableItem()
                    {
                        ShortName = GlobalOptionsEntityShortName,
                        Description = "تنظیمات عمومی",
                        Operations =
                        [
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                        ]
                    },
                    new SecurableItem()
                    {
                        ShortName = WorkspaceEntityShortName,
                        Description = "فضاهای کاری",
                        Operations =
                        [
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                        ]
                    },
                    new SecurableItem()
                    {
                        ShortName = NotificationEntityShortName,
                        Description = "اعلان‌ها",
                        Operations =
                        [
                            new SecurableItemOperation(AddOperationShortName, "ارسال اعلان برای کاربر خاص", false),
                            new SecurableItemOperation(BulkOpertaionShortName, "ارسال اعلان برای همهٔ کاربران", false),
                        ]
                    }

                ];
            }
        }

        /// <summary>
        /// workspace forms and their permissions
        /// </summary>
        public static SecurableItem[] WorkspaceItems
        {
            get
            {
                return
                [
                    new SecurableItem()
                    {
                        ShortName = WorkspaceEntityShortName,
                        Description = "فضاهای کاری",
                        Operations =
                        [
                            new SecurableItemOperation(ModifyOperationShortName, "ویرایش", false),
                            new SecurableItemOperation(InviteMembersOperationShortName, "دعوت عضو جدید", false),
                            new SecurableItemOperation(RemoveMembersOperationShortName, "حذف عضو", false),
                            new SecurableItemOperation(ChangeMemberStatusOperationShortName, "تغییر وضعیت عضو", false),
                            new SecurableItemOperation(ChangeMemberRoleShortName, "تغییر نقش عضو", false),
                            new SecurableItemOperation(QueryMembersListOperationShortName, "مشاهدهٔ فهرست اعضا", false),

                        ]
                    },
                     new SecurableItem()
                    {
                        ShortName = WorkspaceRoleEntityShortName,
                        Description = "نقش‌های فضای کاری",
                        Operations =
                        [
                            new SecurableItemOperation(ViewOperationShortName, "مشاهده", false),
                            new SecurableItemOperation(AddOperationShortName, "ایجاد", false),
                            new SecurableItemOperation(ModifyOperationShortName, "اصلاح", false),
                            new SecurableItemOperation(DeleteOperationShortName, "حذف", false),
                        ]
                    },
                ];
            }
        }

        /// <summary>
        /// Short Name
        /// </summary>
        /// <example>user</example>
        public string ShortName { get; set; }

        /// <summary>
        /// Descripttion
        /// </summary>
        /// <example>کاربران</example>
        public string Description { get; set; }

        /// <summary>
        /// Operations (short name + description + has permission)
        /// </summary>
        /// <example>[
        ///     [view, مشاهده, true],
        ///     [add, ایجاد, false]
        /// ]</example>
        public SecurableItemOperation[] Operations { get; set; }


    }
}
