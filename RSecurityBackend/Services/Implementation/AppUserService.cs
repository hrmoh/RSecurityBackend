using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Audit.Db;
using Microsoft.Extensions.Configuration;
using RSecurityBackend.Models.Cloud;
using Microsoft.AspNetCore.Identity.UI.Services;
using RSecurityBackend.Models.Notification;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Authentication Service
    /// </summary>
    public class AppUserService : IAppUserService
    {

        /// <summary>
        /// Login user, if failed return LoggedOnUserModel is null. <see cref="LoginViewModel.Username"/>
        /// is resolved, in order: as a UserName, then as a confirmed email address, then as a
        /// confirmed phone number. An unconfirmed email/phone number cannot be used to log in.
        /// </summary>
        /// <param name="loginViewModel"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<LoggedOnUserModel>> Login(LoginViewModel loginViewModel, string clientIPAddress)
        {
            if (string.IsNullOrEmpty(loginViewModel.Username))
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "نام کاربری خالی است." : "Username is empty.");
            if (string.IsNullOrEmpty(loginViewModel.Password))
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "رمز خالی است." : "Password is empty.");
            if (bool.Parse(Configuration["AuditNetEnabled"]))
            {
                //we ignore loginViewModel in automatic auditing to prevent logging password data, so we would add a manual auditing to have enough data on login intrusion and ...
                REvent log = new REvent()
                {
                    EventType = "AppUser/Login (POST)(Manual)",
                    StartDate = DateTime.UtcNow,
                    UserName = loginViewModel.Username,
                    IpAddress = clientIPAddress
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            RServiceResult<bool> checkUserExists = await EnsureDefaultUserExists();
            if (!checkUserExists.Result)
            {
                return new RServiceResult<LoggedOnUserModel>(null, checkUserExists.ExceptionString);
            }

            RAppUser appUser = await _userManager.FindByNameAsync(loginViewModel.Username);

            if (appUser == null)
            {
                //fall back to a confirmed email address - UserName is always the channel the
                //account was originally verified through (or later swapped to via a verified
                //ChangeContact), so it never needs this confirmation check; a value only reaches
                //here via the Email/PhoneNumber columns, which can legitimately be unconfirmed
                //(e.g. the optional secondary phone number captured at email signup), so those
                //must not be usable to log in until verified
                RAppUser userByEmail = await _userManager.FindByEmailAsync(loginViewModel.Username);
                if (userByEmail != null && userByEmail.EmailConfirmed)
                {
                    appUser = userByEmail;
                }
            }

            if (appUser == null)
            {
                //same reasoning as above, for a confirmed phone number
                RAppUser userByPhone = await _userManager.Users.Where(u => u.PhoneNumber == loginViewModel.Username).SingleOrDefaultAsync();
                if (userByPhone != null && userByPhone.PhoneNumberConfirmed)
                {
                    appUser = userByPhone;
                }
            }

            if (appUser == null)
            {
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "نام کاربری و/یا رمز نادرست است." : "Username or password is incorrect.");
            }


            var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginViewModel.Password, true);
            if (result.IsLockedOut)
            {
                return new RServiceResult<LoggedOnUserModel>(null,
                    loginViewModel.Language.StartsWith("fa") ?
                    $"نام کاربری شما به دلیل ورود متوالی {_signInManager.Options.Lockout.MaxFailedAccessAttempts} بارهٔ رمزهای اشتباه قفل شده است. لطفاً {_signInManager.Options.Lockout.DefaultLockoutTimeSpan.TotalMinutes} دقیقهٔ‌ دیگر مجدداً تلاش کنید."
                    :
                    $"Your user has been locked out dut to {_signInManager.Options.Lockout.MaxFailedAccessAttempts} failed login attempts. Please check again {_signInManager.Options.Lockout.DefaultLockoutTimeSpan.TotalMinutes} minutes later again."
                    );
            }

            if (result.IsNotAllowed || result.RequiresTwoFactor)
            {
                return new RServiceResult<LoggedOnUserModel>(null, result.ToString());
            }

            if (!result.Succeeded)
            {
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "نام کاربری و/یا رمز نادرست است." : "Username or password is empty.");
            }

            if (appUser.Status == RAppUserStatus.Inactive)
            {
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "نام کاربری شما غیرفعال شده است." : "You user is deactivated.");
            }

            //an account created while AllowUnverifiedEmailSignUp/AllowUnverifiedPhoneSignUp was
            //on has UserName == Email or UserName == PhoneNumber with the matching Confirmed
            //flag left false (see FinalizeSignUp). If that flag is off NOW, block login here -
            //otherwise nothing in this method would ever look at confirmation status for the
            //UserName lookup, and turning the setting back off would silently do nothing. The
            //only way back in at that point is completing ForgotPassword/ResetPassword, which
            //also sets the matching Confirmed flag to true on success.
            if (appUser.UserName == appUser.Email && !appUser.EmailConfirmed && !AllowUnverifiedEmailSignUp)
            {
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "ایمیل شما هنوز تایید نشده است. لطفاً از گزینهٔ فراموشی رمز عبور برای تایید و بازیابی دسترسی استفاده کنید." : "Your email has not been verified yet. Please use forgot password to verify and regain access.");
            }
            if (appUser.UserName == appUser.PhoneNumber && !appUser.PhoneNumberConfirmed && !AllowUnverifiedPhoneSignUp)
            {
                return new RServiceResult<LoggedOnUserModel>(null, loginViewModel.Language.StartsWith("fa") ? "شماره تلفن شما هنوز تایید نشده است. لطفاً از گزینهٔ فراموشی رمز عبور برای تایید و بازیابی دسترسی استفاده کنید." : "Your phone number has not been verified yet. Please use forgot password to verify and regain access.");
            }

            return await IssueSessionAsync(appUser, clientIPAddress, loginViewModel.ClientAppName, loginViewModel.Language);
        }

        /// <summary>
        /// create a new session for an already-authenticated user and issue a JWT for it -
        /// shared tail used by both <see cref="Login"/> (password auth) and
        /// <see cref="ExternalLogin"/> (external provider auth)
        /// </summary>
        /// <param name="appUser"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private async Task<RServiceResult<LoggedOnUserModel>> IssueSessionAsync(RAppUser appUser, string clientIPAddress, string clientAppName, string language)
        {
            RServiceResult<SecurableItem[]> securableItems = await GetUserSecurableItemsStatus(appUser.Id);
            if (!string.IsNullOrEmpty(securableItems.ExceptionString))
                return new RServiceResult<LoggedOnUserModel>(null, securableItems.ExceptionString);

            RTemporaryUserSession userSession =
                new RTemporaryUserSession()
                {
                    RAppUserId = appUser.Id,
                    ClientIPAddress = clientIPAddress,
                    ClientAppName = clientAppName,
                    Language = language,
                    LoginTime = DateTime.Now,
                    LastRenewal = DateTime.Now,
                    ValidUntil = DateTime.Now + TimeSpan.FromSeconds(DefaultTokenExpirationInSeconds),
                    Token = ""
                };


            await _context.Sessions.AddAsync(userSession);

            await _context.SaveChangesAsync();

            //always use the account's canonical UserName for the token, never whatever
            //string the caller happened to authenticate with (which, since Login now accepts
            //a confirmed email or confirmed phone number too, is not necessarily the same value)
            RServiceResult<string> userToken = await GenerateToken(appUser.UserName, appUser.Id, userSession.Id, language);
            if (userToken.Result == null)
            {
                return new RServiceResult<LoggedOnUserModel>(null, userToken.ExceptionString);
            }
            userSession.Token = userToken.Result;
            _context.Sessions.Update(userSession);
            _context.SaveChanges();

            return
                new RServiceResult<LoggedOnUserModel>(
                new LoggedOnUserModel()
                {
                    SessionId = userSession.Id,
                    User = new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SurName = appUser.SurName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed,
                        CreateDate = appUser.CreateDate,
                    },
                    Token = userToken.Result,
                    SecurableItem = securableItems.Result
                }
                );
        }

        /// <summary>
        /// login (or, on first use, auto-signup + link) using an already-verified external
        /// identity provider (e.g. Google). The caller (controller) is responsible for
        /// validating the raw provider token/credential BEFORE calling this - by the time
        /// <paramref name="payload"/> reaches here it is trusted at face value.
        /// </summary>
        /// <param name="payload">verified claims extracted from the provider's token</param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<LoggedOnUserModel>> ExternalLogin(ExternalAuthPayload payload, string clientIPAddress, string clientAppName, string language)
        {
            try
            {
                if (payload == null || string.IsNullOrEmpty(payload.Provider) || string.IsNullOrEmpty(payload.ProviderKey))
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "invalid external auth payload");
                }

                if (string.IsNullOrEmpty(clientIPAddress))
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "client ip address is empty");
                }

                if (string.IsNullOrEmpty(clientAppName))
                {
                    return new RServiceResult<LoggedOnUserModel>(null, "client app name is empty");
                }

                if (bool.Parse(Configuration["AuditNetEnabled"]))
                {
                    REvent log = new REvent()
                    {
                        EventType = $"AppUser/ExternalLogin (POST)(Manual) via {payload.Provider}",
                        StartDate = DateTime.UtcNow,
                        UserName = payload.Email,
                        IpAddress = clientIPAddress
                    };
                    _context.AuditLogs.Add(log);
                    await _context.SaveChangesAsync();
                }

                RServiceResult<bool> checkUserExists = await EnsureDefaultUserExists();
                if (!checkUserExists.Result)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, checkUserExists.ExceptionString);
                }

                //1) already linked to this exact provider identity?
                RAppUser appUser = await _userManager.FindByLoginAsync(payload.Provider, payload.ProviderKey);

                if (appUser == null)
                {
                    //2) not linked yet - if the provider vouches for a verified email that
                    //matches an existing (also confirmed) local account, link to that account
                    //instead of creating a duplicate. Only ever done when the provider itself
                    //reports the email as verified - otherwise this would be an account
                    //takeover vector (sign up elsewhere using someone else's email, "link" here)
                    if (!string.IsNullOrEmpty(payload.Email) && payload.EmailVerified)
                    {
                        RAppUser existingByEmail = await _userManager.FindByEmailAsync(payload.Email);
                        if (existingByEmail != null && existingByEmail.EmailConfirmed)
                        {
                            appUser = existingByEmail;
                        }
                    }

                    if (appUser == null)
                    {
                        //3) no existing account at all - auto-signup, same trust rules as (2):
                        //requires the provider to report a verified email
                        if (string.IsNullOrEmpty(payload.Email))
                        {
                            return new RServiceResult<LoggedOnUserModel>(null, (language ?? "").StartsWith("fa") ? "ارائه‌دهنده، ایمیلی برای این حساب کاربری ارسال نکرد." : "Identity provider did not supply an email address.");
                        }
                        if (!payload.EmailVerified)
                        {
                            return new RServiceResult<LoggedOnUserModel>(null, (language ?? "").StartsWith("fa") ? "ایمیل شما توسط ارائه‌دهنده تایید نشده است." : "Your email is not verified by the identity provider.");
                        }

                        string firstName = string.IsNullOrEmpty(payload.FirstName) ? payload.DisplayName : payload.FirstName;
                        if (string.IsNullOrEmpty(firstName))
                        {
                            firstName = payload.Email;
                        }

                        RegisterRAppUser newUserInfo = new RegisterRAppUser()
                        {
                            Username = payload.Email,
                            Email = payload.Email,
                            //external-provider accounts don't need a usable local password;
                            //AddUser already generates one when none is supplied
                            Password = null,
                            Status = RAppUserStatus.Active,
                            IsAdmin = false,
                            FirstName = firstName,
                            SurName = payload.SurName,
                            NickName = string.IsNullOrEmpty(payload.DisplayName) ? firstName : payload.DisplayName,
                        };

                        RServiceResult<RAppUser> addResult = await AddUser(newUserInfo);
                        if (addResult.Result == null)
                        {
                            return new RServiceResult<LoggedOnUserModel>(null, addResult.ExceptionString);
                        }
                        appUser = addResult.Result;
                    }

                    IdentityResult linkResult = await _userManager.AddLoginAsync(appUser, new UserLoginInfo(payload.Provider, payload.ProviderKey, payload.DisplayName));
                    if (!linkResult.Succeeded)
                    {
                        return new RServiceResult<LoggedOnUserModel>(null, ErrorsToString(linkResult.Errors));
                    }
                }

                if (appUser.Status == RAppUserStatus.Inactive)
                {
                    return new RServiceResult<LoggedOnUserModel>(null, (language ?? "").StartsWith("fa") ? "نام کاربری شما غیرفعال شده است." : "You user is deactivated.");
                }

                return await IssueSessionAsync(appUser, clientIPAddress, clientAppName, language);
            }
            catch (Exception exp)
            {
                return new RServiceResult<LoggedOnUserModel>(null, exp.ToString());
            }
        }

        /// <summary>
        /// replace a (probably expired session) with a new one
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<LoggedOnUserModel>> ReLogin(Guid sessionId, string clientIPAddress)
        {

            RTemporaryUserSession oldSession = await _context.Sessions.Include(s => s.RAppUser).Where(s => s.Id == sessionId).SingleOrDefaultAsync();
            if (oldSession == null)
            {
                return new RServiceResult<LoggedOnUserModel>(null, "Invalid session");
            }
            RAppUser appUser = oldSession.RAppUser;
            if (appUser.Status == RAppUserStatus.Inactive)
            {
                return new RServiceResult<LoggedOnUserModel>(null, oldSession.Language.StartsWith("fa") ? "یکی از مدیران کاربر را غیرفعال کرده است." : "User is disabled by an admin.");
            }
            RServiceResult<SecurableItem[]> securableItems = await GetUserSecurableItemsStatus(appUser.Id);
            if (!string.IsNullOrEmpty(securableItems.ExceptionString))
                return new RServiceResult<LoggedOnUserModel>(null, securableItems.ExceptionString);

            RTemporaryUserSession newSession =
                new RTemporaryUserSession()
                {
                    RAppUserId = appUser.Id,
                    ClientIPAddress = clientIPAddress,
                    ClientAppName = oldSession.ClientAppName,
                    Language = oldSession.Language,
                    LoginTime = DateTime.Now,
                    LastRenewal = DateTime.Now,
                    ValidUntil = DateTime.Now + TimeSpan.FromSeconds(DefaultTokenExpirationInSeconds),
                    Token = ""
                };


            await _context.Sessions.AddAsync(newSession);
            _context.Sessions.Remove(oldSession);

            await _context.SaveChangesAsync();

            RServiceResult<string> userToken = await GenerateToken(appUser.UserName, appUser.Id, newSession.Id, oldSession.Language);
            if (userToken.Result == null)
            {
                return new RServiceResult<LoggedOnUserModel>(null, userToken.ExceptionString);
            }
            newSession.Token = userToken.Result;
            _context.Sessions.Update(newSession);
            _context.SaveChanges();

            return
                new RServiceResult<LoggedOnUserModel>(
                new LoggedOnUserModel()
                {
                    SessionId = newSession.Id,
                    User = new PublicRAppUser()
                    {
                        Id = appUser.Id,
                        Username = appUser.UserName,
                        Email = appUser.Email,
                        FirstName = appUser.FirstName,
                        SurName = appUser.SurName,
                        PhoneNumber = appUser.PhoneNumber,
                        RImageId = appUser.RImageId,
                        Status = appUser.Status,
                        NickName = appUser.NickName,
                        Website = appUser.Website,
                        Bio = appUser.Bio,
                        EmailConfirmed = appUser.EmailConfirmed,
                        CreateDate = appUser.CreateDate,
                    },
                    Token = userToken.Result,
                    SecurableItem = securableItems.Result
                }
                );

        }

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddUserToRole(Guid userId, string roleName)
        {

            RAppUser dbUserInfo =
                await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();

            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, $"کاربر مورد نظر با ایمیل {userId} پیدا نشد ");
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var identityResult = await _roleManager.CreateAsync(new RAppRole(roleName));
                if (!identityResult.Succeeded)
                {
                    return new RServiceResult<bool>(false, $"Error creating {roleName} role : " + ErrorsToString(identityResult.Errors));
                }
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(dbUserInfo, roleName);
            if (!addToRoleResult.Succeeded)
            {
                return new RServiceResult<bool>(false, $"Error adding admin to {roleName} role : " + ErrorsToString(addToRoleResult.Errors));
            }
            return new RServiceResult<bool>(true);

        }


        /// <summary>
        /// Logout
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> Logout(Guid userId, Guid sessionId)
        {
            RTemporaryUserSession session =
                await _context.Sessions
                .Where(s => s.Id == sessionId && s.RAppUserId == userId)
                .FirstOrDefaultAsync();
            if (session == null)
                return new RServiceResult<bool>(false, "session is invalid");
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return new RServiceResult<bool>(true);
        }


        /// <summary>
        /// Does Session exist?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> SessionExists(Guid userId, Guid sessionId)
        {
            return new RServiceResult<bool>(
                await _context.Sessions
                .Where(s => s.RAppUserId == userId && s.Id == sessionId)
                .FirstOrDefaultAsync() != null
                );
        }



        /// <summary>
        /// returns user information
        /// </summary>
        /// <remarks>
        /// PasswordHash becomes empty
        /// </remarks>
        /// <param name="userId"></param>        
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser>> GetUserInformation(Guid userId)
        {
            RAppUser appUser =
                await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();
            if (appUser == null)
                return new RServiceResult<PublicRAppUser>(null);
            return new RServiceResult<PublicRAppUser>(
                new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SurName = appUser.SurName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed,
                    CreateDate = appUser.CreateDate,
                });
        }


        /// <summary>
        /// all users informations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterByEmail"></param>
        /// <param name="filterByNickName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<(PaginationMetadata PagingMeta, PublicRAppUser[] Items)>>
            GetAllUsersInformation(PagingParameterModel paging, string filterByEmail, string filterByNickName)
        {
            var source = _userManager.Users
                .Where(appUser =>

                (string.IsNullOrEmpty(filterByEmail) || (!string.IsNullOrEmpty(filterByEmail) && appUser.Email.Contains(filterByEmail)))

                &&

                (string.IsNullOrEmpty(filterByNickName) || (!string.IsNullOrEmpty(filterByNickName) && appUser.NickName.Contains(filterByNickName)))

                )
                .Select(appUser => new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SurName = appUser.SurName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed,
                    CreateDate = appUser.CreateDate,
                });

            return new RServiceResult<(PaginationMetadata PagingMeta, PublicRAppUser[] Items)>(
                await QueryablePaginator<PublicRAppUser>.Paginate(source, paging));


        }

        /// <summary>
        /// Get User Sessions
        /// </summary>
        /// <param name="userId">if null is passed returns all sessions</param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRUserSession[]>> GetUserSessions(Guid? userId)
        {

            List<PublicRUserSession> publicRUserSessions = new List<PublicRUserSession>();

            RTemporaryUserSession[] sessions =
                userId == null ?
                await _context.Sessions.ToArrayAsync()
                :
                await _context.Sessions.Where(s => s.RAppUserId == userId).ToArrayAsync()
                ;

            foreach (RTemporaryUserSession rUserSession in sessions)
                publicRUserSessions.Add(
                    new PublicRUserSession()
                    {
                        Id = rUserSession.Id,
                        RAppUser = new PublicRAppUser()
                        {
                            Id = rUserSession.RAppUser.Id,
                            Username = rUserSession.RAppUser.UserName,
                            Email = rUserSession.RAppUser.Email,
                            FirstName = rUserSession.RAppUser.FirstName,
                            SurName = rUserSession.RAppUser.SurName,
                            PhoneNumber = rUserSession.RAppUser.PhoneNumber,
                            RImageId = rUserSession.RAppUser.RImageId,
                            Status = rUserSession.RAppUser.Status,
                            NickName = rUserSession.RAppUser.NickName,
                            Website = rUserSession.RAppUser.Website,
                            Bio = rUserSession.RAppUser.Bio,
                            EmailConfirmed = rUserSession.RAppUser.EmailConfirmed,
                            CreateDate = rUserSession.RAppUser.CreateDate,
                        },
                        ClientAppName = rUserSession.ClientAppName,
                        ClientIPAddress = rUserSession.ClientIPAddress,
                        Language = rUserSession.Language,
                        LastRenewal = rUserSession.LastRenewal,
                        LoginTime = rUserSession.LoginTime
                    }
                    );

            return new RServiceResult<PublicRUserSession[]>(publicRUserSessions.ToArray());


        }

        /// <summary>
        /// Get User Session
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRUserSession>> GetUserSession(Guid userId, Guid sessionId)
        {
            RTemporaryUserSession rUserSession =
                await _context.Sessions.Include(s => s.RAppUser).Where(s => s.RAppUserId == userId && s.Id == sessionId).SingleOrDefaultAsync();
            if (rUserSession == null)
            {
                return null;
            }
            return new RServiceResult<PublicRUserSession>
                (
                    new PublicRUserSession()
                    {
                        Id = rUserSession.Id,
                        RAppUser = new PublicRAppUser()
                        {
                            Id = rUserSession.RAppUser.Id,
                            Username = rUserSession.RAppUser.UserName,
                            Email = rUserSession.RAppUser.Email,
                            FirstName = rUserSession.RAppUser.FirstName,
                            SurName = rUserSession.RAppUser.SurName,
                            PhoneNumber = rUserSession.RAppUser.PhoneNumber,
                            RImageId = rUserSession.RAppUser.RImageId,
                            Status = rUserSession.RAppUser.Status,
                            NickName = rUserSession.RAppUser.NickName,
                            Website = rUserSession.RAppUser.Website,
                            Bio = rUserSession.RAppUser.Bio,
                            EmailConfirmed = rUserSession.RAppUser.EmailConfirmed,
                            CreateDate = rUserSession.RAppUser.CreateDate,
                        },
                        ClientAppName = rUserSession.ClientAppName,
                        ClientIPAddress = rUserSession.ClientIPAddress,
                        Language = rUserSession.Language,
                        LastRenewal = rUserSession.LastRenewal,
                        LoginTime = rUserSession.LoginTime
                    }

            );
        }

        /// <summary>
        /// is user admin?
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsAdmin(Guid userId)
        {

            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            bool res = await _userManager.IsInRoleAsync(dbUserInfo, _userRoleService.AdministratorRoleName);
            return new RServiceResult<bool>(res);

        }

        /// <summary>
        /// is user in either of passed roles?
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> IsInRoles(Guid userId, string[] roleNames)
        {

            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            foreach (string roleName in roleNames)
            {
                if (await _userManager.IsInRoleAsync(dbUserInfo, roleName))
                {
                    return new RServiceResult<bool>(true);
                }
            }

            return new RServiceResult<bool>(false);

        }

        /// <summary>
        /// Get User Roles
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<IList<string>>> GetUserRoles(Guid userId)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<IList<string>>(null, "کاربر مورد نظر یافت نشد");
            }

            return new RServiceResult<IList<string>>(await _userManager.GetRolesAsync(dbUserInfo));

        }

        /// <summary>
        /// remove user from role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveFromRole(Guid id, string role)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(id.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            IdentityResult result = await _userManager.RemoveFromRoleAsync(dbUserInfo, role);
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> AddToRole(Guid id, string role)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(id.ToString());
            if (dbUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            IdentityResult result = await _userManager.AddToRoleAsync(dbUserInfo, role);
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
            }

            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// Lists user permissions
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<SecurableItem[]>> GetUserSecurableItemsStatus(Guid userId)
        {
            SecurableItem[] securableItems = _userRoleService.GetSecurableItems();
            RServiceResult<IList<string>> roles = await GetUserRoles(userId);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return new RServiceResult<SecurableItem[]>(null, roles.ExceptionString);

            bool isAdmin = (await IsAdmin(userId)).Result;

            foreach (SecurableItem securableItem in securableItems)
            {
                foreach (SecurableItemOperation operation in securableItem.Operations)
                {
                    foreach (string role in roles.Result)
                    {
                        RServiceResult<bool> hasPermission = await _userRoleService.HasPermission(role, securableItem.ShortName, operation.ShortName);
                        if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
                            return new RServiceResult<SecurableItem[]>(null, hasPermission.ExceptionString);
                        if (isAdmin || hasPermission.Result)
                        {
                            operation.Status = true;
                        }
                    }
                }
            }
            return new RServiceResult<SecurableItem[]>(securableItems);
        }

        /// <summary>
        /// Has user specified permission
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> HasPermission(Guid userId, string securableItemShortName, string operationShortName)
        {
            RServiceResult<IList<string>> roles = await GetUserRoles(userId);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return new RServiceResult<bool>(false, roles.ExceptionString);

            foreach (string role in roles.Result)
            {
                RServiceResult<bool> hasPermission = await _userRoleService.HasPermission(role, securableItemShortName, operationShortName);
                if (!string.IsNullOrEmpty(hasPermission.ExceptionString))
                    return new RServiceResult<bool>(false, hasPermission.ExceptionString);
                if (hasPermission.Result)
                {
                    return new RServiceResult<bool>(true);
                }
            }

            return
                new RServiceResult<bool>
                (
                    false
                );
        }

        /// <summary>
        /// all users having a certain permission
        /// </summary>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser[]>> GetUsersHavingPermission(string securableItemShortName, string operationShortName)
        {
            var rolesResult = await _userRoleService.GetRolesHavingPermission(securableItemShortName, operationShortName);
            if (!string.IsNullOrEmpty(rolesResult.ExceptionString))
            {
                return new RServiceResult<PublicRAppUser[]>(null, rolesResult.ExceptionString);
            }
            List<PublicRAppUser> lstPublicUsersInfo = new List<PublicRAppUser>();

            RAppRole[] roles = rolesResult.Result;
            foreach (RAppRole role in roles)
            {
                var usersInRole = _userManager.GetUsersInRoleAsync(role.Name);
                foreach (var appUser in usersInRole.Result)
                {
                    if (lstPublicUsersInfo.Where(u => u.Id == appUser.Id).FirstOrDefault() == null)
                    {
                        lstPublicUsersInfo.Add(
                            new PublicRAppUser()
                            {
                                Id = appUser.Id,
                                Username = appUser.UserName,
                                Email = appUser.Email,
                                FirstName = appUser.FirstName,
                                SurName = appUser.SurName,
                                PhoneNumber = appUser.PhoneNumber,
                                RImageId = appUser.RImageId,
                                Status = appUser.Status,
                                NickName = appUser.NickName,
                                Website = appUser.Website,
                                Bio = appUser.Bio,
                                EmailConfirmed = appUser.EmailConfirmed,
                                CreateDate = appUser.CreateDate,
                            });
                    }
                }
            }

            return new RServiceResult<PublicRAppUser[]>(lstPublicUsersInfo.ToArray());
        }


        /// <summary>
        /// add a new user
        /// </summary>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RAppUser>> AddUser(RegisterRAppUser newUserInfo)
        {
            if (!newUserInfo.IsAdmin)
            {
                RServiceResult<bool> checkUserExists = await EnsureDefaultUserExists();
                if (!checkUserExists.Result)
                {
                    return new RServiceResult<RAppUser>(null, checkUserExists.ExceptionString);
                }
            }

            RAppUser existingInfo = await _userManager.FindByNameAsync(newUserInfo.Username);
            if (existingInfo != null)
            {
                return new RServiceResult<RAppUser>(null, "username is already taken");
            }

            if (string.IsNullOrEmpty(newUserInfo.Password))
            {
                newUserInfo.Password = Guid.NewGuid().ToString();
            }


            RAppUser newDbUser =
                new RAppUser()
                {
                    UserName = newUserInfo.Username,
                    FirstName = newUserInfo.FirstName,
                    SurName = newUserInfo.SurName,
                    NickName = newUserInfo.NickName,
                    Email = newUserInfo.Email,
                    //an email/phone number entered directly by an admin (this endpoint) has no
                    //OTP verification step of its own, so we treat it as pre-verified - same trust
                    //level as any other field an admin sets here. This also keeps these values
                    //usable for login, since Login() requires EmailConfirmed/PhoneNumberConfirmed
                    //for anyone looking up by email/phone rather than by UserName.
                    EmailConfirmed = !string.IsNullOrEmpty(newUserInfo.Email),
                    PhoneNumber = newUserInfo.PhoneNumber,
                    PhoneNumberConfirmed = !string.IsNullOrEmpty(newUserInfo.PhoneNumber),
                    CreateDate = DateTime.Now,
                    Status = newUserInfo.Status

                };


            var result = await _userManager.CreateAsync(newDbUser, newUserInfo.Password);

            if (!result.Succeeded)
            {
                return new RServiceResult<RAppUser>(null, ErrorsToString(result.Errors));
            }

            newUserInfo.Id = newDbUser.Id;

            if (newUserInfo.IsAdmin)
            {

                if (!await _roleManager.RoleExistsAsync(_userRoleService.AdministratorRoleName))
                {
                    var roleCheckResult = await _roleManager.CreateAsync(new RAppRole(_userRoleService.AdministratorRoleName));
                    if (!roleCheckResult.Succeeded)
                    {
                        return new RServiceResult<RAppUser>(null, "Error creating Administrator role : " + ErrorsToString(roleCheckResult.Errors));
                    }
                }


                var addToAdminRoleResult = await _userManager.AddToRoleAsync(newDbUser, _userRoleService.AdministratorRoleName);
                if (!addToAdminRoleResult.Succeeded)
                {
                    return new RServiceResult<RAppUser>(null, $"Error adding {newDbUser.UserName} to Administrator role : " + ErrorsToString(addToAdminRoleResult.Errors));
                }
            }


            return new RServiceResult<RAppUser>(newDbUser);
        }


        /// <summary>
        /// modify existing user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserInfo"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ModifyUser(Guid userId, RegisterRAppUser updateUserInfo)
        {
            RAppUser existingInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (existingInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            RServiceResult<bool> isAdmin = await IsAdmin(userId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
            {
                return new RServiceResult<bool>(false, isAdmin.ExceptionString);
            }

            if (isAdmin.Result && !updateUserInfo.IsAdmin)
            {
                List<RAppUser> adminUsers = new List<RAppUser>(await _userManager.GetUsersInRoleAsync(_userRoleService.AdministratorRoleName));
                int nActiveAdminUsers = 0;
                foreach (RAppUser adminUser in adminUsers)
                {
                    if (adminUser.Status == RAppUserStatus.Active)
                        nActiveAdminUsers++;
                }
                if (nActiveAdminUsers <= 1)
                {
                    return new RServiceResult<bool>(false, "You cannot reduce number of active admin users to 0.");
                }
            }

            if (isAdmin.Result != updateUserInfo.IsAdmin)
            {
                return new RServiceResult<bool>(false, "امکان تغییر وضعیت مدیر کاربر از طریق این تابع وجود ندارد.");
            }




            if (existingInfo.UserName != updateUserInfo.Username)
            {

                RAppUser anotheruserWithUserName = await _userManager.FindByNameAsync(updateUserInfo.Username);

                if (anotheruserWithUserName != null)
                {
                    return new RServiceResult<bool>(false, "نام کاربری تکراری می باشد");
                }

                existingInfo.UserName = updateUserInfo.Username;
            }

            if (updateUserInfo.RImageId != null && updateUserInfo.RImageId != Guid.Empty && updateUserInfo.RImageId != existingInfo.RImageId)
            {
                return new RServiceResult<bool>(false, "برای تغییر تصویر کاربر از تابع اختصاصی این کار استفاده کنید");
            }

            updateUserInfo.FirstName = string.IsNullOrEmpty(updateUserInfo.FirstName) ? updateUserInfo.FirstName : updateUserInfo.FirstName.Trim();
            updateUserInfo.SurName = string.IsNullOrEmpty(updateUserInfo.SurName) ? updateUserInfo.SurName : updateUserInfo.SurName.Trim();
            updateUserInfo.NickName = string.IsNullOrEmpty(updateUserInfo.NickName) ? updateUserInfo.NickName : updateUserInfo.NickName.Trim();

            if (string.IsNullOrEmpty(updateUserInfo.NickName) && string.IsNullOrEmpty(updateUserInfo.FirstName) && string.IsNullOrEmpty(updateUserInfo.SurName))
            {
                return new RServiceResult<bool>(false, "نام، نام خانوادگی و نام مستعار نمی‌توانند همگی خالی باشند.");
            }





            existingInfo.FirstName = updateUserInfo.FirstName;
            existingInfo.SurName = updateUserInfo.SurName;
            existingInfo.Email = updateUserInfo.Email;
            existingInfo.PhoneNumber = updateUserInfo.PhoneNumber;
            existingInfo.Status = updateUserInfo.Status;
            existingInfo.Bio = updateUserInfo.Bio;
            existingInfo.NickName = updateUserInfo.NickName;
            existingInfo.Website = updateUserInfo.Website;

            if (!string.IsNullOrEmpty(updateUserInfo.Password))
            {
                foreach (var passwordValidator in _userManager.PasswordValidators)
                {
                    var resPass = await passwordValidator.ValidateAsync(_userManager, existingInfo, updateUserInfo.Password);
                    if (!resPass.Succeeded)
                    {
                        return new RServiceResult<bool>(false, ErrorsToString(resPass.Errors));
                    }
                }

                existingInfo.PasswordHash = _userManager.PasswordHasher.HashPassword(existingInfo, updateUserInfo.Password);
            }

            if (updateUserInfo.Status == RAppUserStatus.Inactive)
            {
                _context.Sessions.RemoveRange(await _context.Sessions.Where(u => u.RAppUserId == userId).ToArrayAsync());
                await _context.SaveChangesAsync();
            }


            var result = await _userManager.UpdateAsync(existingInfo);
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
            }

            //updating admin status  is not supported               



            return new RServiceResult<bool>(true);
        }

        /// <summary>
        /// change user password checking old password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ChangePassword(Guid userId, string oldPassword, string newPassword)
        {
            RAppUser appUser = await _userManager.FindByIdAsync(userId.ToString());

            if (appUser == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            var result = await _userManager.ChangePasswordAsync(appUser, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                return new RServiceResult<bool>(false, "Identity error details says: " + result.ToString());
            }
            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// remove user data
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> RemoveUserData(Guid userId)
        {
            try
            {
                var memberships = await _context.RWSUsers.Where(m => m.RAppUserId == userId).ToListAsync();
                if (memberships.Any())
                {
                    if (memberships.Where(m => m.Status == RWSUserMembershipStatus.Owner).Any())
                    {
                        return new RServiceResult<bool>(false, $"شما مالک شرکت {memberships.Where(m => m.Status == RWSUserMembershipStatus.Owner).Count()} شرکت هستید. قبل از حذف حساب کاربری لازم است این شرکت‌ها را حذف کنید یا مالکیت آنها را به کاربر دیگری واگذار کنید.");
                    }
                    _context.RemoveRange(memberships);
                }

                var rwsUsers = await _context.RWSUsers.Where(c => c.RAppUserId == userId).ToListAsync();
                _context.RemoveRange(rwsUsers);

                var notications = await _context.Notifications.Where(n => n.UserId == userId).ToArrayAsync();
                _context.RemoveRange(notications);

                var options = await _context.Options.Where(o => o.RAppUserId == userId).ToListAsync();
                _context.RemoveRange(options);

                var invitations = await _context.WorkspaceUserInvitations.Where(w => w.UserId == userId).ToListAsync();
                if (invitations.Any())
                {
                    _context.RemoveRange(invitations);
                }
                var userRoles = await _context.RWSUserRoles.Where(w => w.UserId == userId).ToListAsync();
                if (userRoles.Any())
                {
                    _context.RemoveRange(userRoles);
                }
                var changelogs = await _context.RChangeLogs.Where(w => w.RAppUserId == userId).ToListAsync();
                if (changelogs.Any())
                {
                    _context.RemoveRange(changelogs);
                }

                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

        }

        /// <summary>
        /// delete user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true if succeeds</returns>
        public virtual async Task<RServiceResult<bool>> DeleteUser(Guid userId)
        {
            RAppUser dbUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (dbUserInfo != null)
            {
                var resDelData = await RemoveUserData(userId);
                if (!string.IsNullOrEmpty(resDelData.ExceptionString))
                    return new RServiceResult<bool>(false, resDelData.ExceptionString);
                if (!resDelData.Result)
                    return new RServiceResult<bool>(false, "حذف اطلاعات کاربر با خطا مواجه شد.");
                var result = await _userManager.DeleteAsync(dbUserInfo);
                if (!result.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(result.Errors));
                }
                return new RServiceResult<bool>(true);
            }
            return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
        }

        /// <summary>
        /// start leaving
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> StartLeaving(Guid userId, Guid sessionId, string clientIPAddress)
        {
            var session = await GetUserSession(userId, sessionId);
            var user = (await GetUserInformation(userId)).Result;

            //checking this queue for previous signup attempts is unnecessary and is not done intentionally
            RVerifyQueueItem item = new RVerifyQueueItem()
            {
                QueueType = RVerifyQueueType.UserSelfDelete,
                Email = user.Email,
                DateTime = DateTime.Now,
                ClientIPAddress = clientIPAddress,
                ClientAppName = session == null ? "Unknown Session" : session.Result.ClientAppName,
                Secret = $"{new Random(DateTime.Now.Millisecond).Next(0, 99999)}".PadLeft(6, '0'),
                Language = session == null ? "Unknown Session Language" : session.Result.Language
            };

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }

            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);

        }

        /// <summary>
        /// Set User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<Guid?>> SetUserImage(Guid userId, IFormFileCollection files)
        {

            RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

            if (files.Count == 0)
            {
                user.RImageId = null;
            }
            else
            {
                if (files.Count != 1)
                {
                    return new RServiceResult<Guid?>(null, "files.Count != 1");
                }

                IFormFile file = files[0];


                int nImageWidth = 192;
                using Stream stream = file.OpenReadStream();
                using Image img = Image.FromStream(stream);
                if (img.Width > nImageWidth)
                {
                    using Bitmap bmpPhase1 = new Bitmap(nImageWidth, nImageWidth);
                    using (Graphics g = Graphics.FromImage(bmpPhase1))
                    {
                        g.DrawImage(img, new Rectangle(0, 0, nImageWidth, img.Height * nImageWidth / img.Height));
                    }

                    using Brush brush = new TextureBrush(bmpPhase1);
                    using Bitmap bmpPhase2 = new Bitmap(nImageWidth, nImageWidth);
                    using (Graphics g = Graphics.FromImage(bmpPhase2))
                    {
                        g.FillEllipse(brush, new Rectangle(0, 0, nImageWidth, nImageWidth));
                    }
                    bmpPhase2.MakeTransparent();

                    using MemoryStream ms = new MemoryStream();
                    bmpPhase2.Save(ms, ImageFormat.Png);

                    ms.Position = 0;
                    RServiceResult<RImage> image = await _imageFileService.Add(null, ms, file.FileName, "UserProfiles");

                    if (!string.IsNullOrEmpty(image.ExceptionString))
                    {
                        return new RServiceResult<Guid?>(null, image.ExceptionString);
                    }
                    user.RImage = image.Result;
                }
                else
                {
                    RServiceResult<RImage> image = await _imageFileService.Add(null, stream, file.FileName, "UserProfiles");

                    if (!string.IsNullOrEmpty(image.ExceptionString))
                    {
                        return new RServiceResult<Guid?>(null, image.ExceptionString);
                    }
                    user.RImage = image.Result;
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new RServiceResult<Guid?>(null, ErrorsToString(result.Errors));
            }

            return new RServiceResult<Guid?>((Guid?)user.RImageId);

        }

        /// <summary>
        /// Get User Image
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RImage>> GetUserImage(Guid userId)
        {
            RAppUser user = await _userManager.FindByIdAsync(userId.ToString());

            if (user.RImageId != null)
            {
                return await _imageFileService.GetImage((Guid)user.RImageId);
            }

            return new RServiceResult<RImage>(null);

        }

        /// <summary>
        /// Start signup process using email or phone number (sms otp).
        /// If <paramref name="email"/> contains an "@" it is treated as an email address
        /// (verification code sent by email via IEmailSender at the controller level),
        /// otherwise it is treated as a phone number (verification code sent by sms via
        /// ISmsSender at the controller level).
        /// </summary>
        /// <param name="email">email address or phone number</param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> SignUp(string email, string clientIPAddress, string clientAppName, string language)
        {

            if (string.IsNullOrEmpty(clientIPAddress))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "client ip address is empty");
            }

            if (string.IsNullOrEmpty(clientAppName))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "client app name is empty");
            }

            if (string.IsNullOrEmpty(email))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "email/phone number is empty");
            }

            bool isEmail = email.Contains('@');

            RAppUser existingUser =
                isEmail
                ?
                await _userManager.FindByEmailAsync(email)
                :
                await _userManager.Users.Where(u => u.PhoneNumber == email).SingleOrDefaultAsync();
            if (existingUser != null)
            {
                return new RServiceResult<RVerifyQueueItem>(null, "شما قبلا ثبت نام کرده‌اید.");
            }

            existingUser = await _userManager.FindByNameAsync(email);
            if (existingUser != null)
            {
                return new RServiceResult<RVerifyQueueItem>(null, "این نام کاربری قبلا استفاده شده است");
            }

            if (!isEmail)
            {
                //sms costs money (unlike email), so we enforce a resend cooldown for the phone case
                RVerifyQueueItem lastAttempt =
                    await _context.VerifyQueueItems
                    .Where(i => i.QueueType == RVerifyQueueType.SignUp && i.PhoneNumber == email)
                    .OrderByDescending(i => i.DateTime)
                    .FirstOrDefaultAsync();
                if (lastAttempt != null && lastAttempt.DateTime > DateTime.Now.AddSeconds(-PhoneSignUpResendCooldownSeconds))
                {
                    double secondsLeft = (lastAttempt.DateTime.AddSeconds(PhoneSignUpResendCooldownSeconds) - DateTime.Now).TotalSeconds;
                    return new RServiceResult<RVerifyQueueItem>(null, $"لطفاً {Math.Ceiling(secondsLeft)} ثانیهٔ دیگر مجدداً تلاش کنید.");
                }
            }

            var oldSecrets = await _context.VerifyQueueItems.Where(i => i.DateTime < DateTime.Now.AddDays(-1)).ToListAsync();
            if (oldSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(oldSecrets);
                await _context.SaveChangesAsync();
            }

            //checking this queue for previous signup attempts is unnecessary and is not done intentionally for the email case
            RVerifyQueueItem item = new RVerifyQueueItem()
            {
                QueueType = RVerifyQueueType.SignUp,
                Email = isEmail ? email : null,
                PhoneNumber = isEmail ? null : email,
                DateTime = DateTime.Now,
                ClientIPAddress = clientIPAddress,
                ClientAppName = clientAppName,
                Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                Language = language
            };

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }

            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);

        }

        /// <summary>
        /// verify signup / forgot password
        /// </summary>
        /// <param name="verifyQueueType"></param>
        /// <param name="secret"></param>
        /// <returns>associated email address or phone number</returns>
        public virtual async Task<RServiceResult<string>> RetrieveEmailFromQueueSecret(RVerifyQueueType verifyQueueType, string secret)
        {

            secret = secret.Trim();
            RVerifyQueueItem item = await _context.VerifyQueueItems.Where(i => i.QueueType == verifyQueueType && i.Secret == secret).SingleOrDefaultAsync();
            if (item == null)
            {
                return new RServiceResult<string>("");
            }

            return new RServiceResult<string>(!string.IsNullOrEmpty(item.Email) ? item.Email : item.PhoneNumber);

        }

        /// <summary>
        /// finalize signup process using email or phone number (sms otp), matching whichever
        /// channel was used in <see cref="SignUp"/> (detected the same way: "@" present == email)
        /// </summary>
        /// <param name="email">email address or phone number (must match what was passed to SignUp)</param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="surName"></param>
        /// <param name="phoneNumber">optional secondary phone number, only used in the email-signup case</param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string surName, string phoneNumber)
        {
            bool isEmail = email.Contains('@');

            RAppUser existingUser =
                isEmail
                ?
                await _userManager.FindByEmailAsync(email)
                :
                await _userManager.Users.Where(u => u.PhoneNumber == email).SingleOrDefaultAsync();
            if (existingUser != null)
            {
                return new RServiceResult<bool>(false, isEmail ? "این آدرس ایمیل قبلا استفاده شده است" : "این شماره تلفن قبلا استفاده شده است");
            }

            existingUser = await _userManager.FindByNameAsync(email);
            if (existingUser != null)
            {
                return new RServiceResult<bool>(false, "این نام کاربری قبلا استفاده شده است");
            }

            if (
                   email
                   !=
                   (await RetrieveEmailFromQueueSecret(RVerifyQueueType.SignUp, secret)).Result
                  )
            {
                return new RServiceResult<bool>(false, isEmail ? "کد ارسالی به ایمیل اشتباه وارد شده است" : "کد ارسالی به شماره تلفن اشتباه وارد شده است");
            }

            secret = secret.Trim();

            firstName = (firstName ?? "").Trim();
            surName = (surName ?? "").Trim();

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(surName))
            {
                return new RServiceResult<bool>(false, "لطفاً حداقل یکی از اطلاعات نام یا نام خانوادگی را وارد کنید.");
            }

            RegisterRAppUser newUserInfo = new RegisterRAppUser()
            {
                Username = email,
                Email = isEmail ? email : null,
                Password = password,
                Status = RAppUserStatus.Active,
                IsAdmin = false,
                FirstName = firstName,
                SurName = surName,
                NickName = $"{firstName} {surName}".Trim(),
                //in the email case phoneNumber is an optional extra profile field; in the phone
                //case the identifier itself (email param) is the phone number
                PhoneNumber = isEmail ? (string.IsNullOrEmpty(phoneNumber) ? null : phoneNumber) : email,
            };

            RServiceResult<RAppUser> userAddResult = await AddUser(newUserInfo);

            if (userAddResult.Result == null)
            {
                return new RServiceResult<bool>(false, userAddResult.ExceptionString);
            }

            //AddUser marks EmailConfirmed/PhoneNumberConfirmed true for any value it's given -
            //correct for its own admin-facing "trusted direct input" use case, but WRONG here:
            //this call site has an exact per-channel verification state that must be preserved:
            //- the channel actually used to complete THIS signup is verified only if the OTP
            //  flow was actually completed (never true when AllowUnverifiedEmailSignUp /
            //  AllowUnverifiedPhoneSignUp let this call skip it)
            //- the other, optional secondary field (e.g. a phone number captured alongside an
            //  email signup) was never verified at all, regardless of the above - overwrite
            //  whatever AddUser assumed rather than leaving its blanket true in place
            bool primaryChannelVerified = isEmail ? !AllowUnverifiedEmailSignUp : !AllowUnverifiedPhoneSignUp;

            userAddResult.Result.EmailConfirmed = isEmail && primaryChannelVerified;
            userAddResult.Result.PhoneNumberConfirmed = !isEmail && primaryChannelVerified;

            await _userManager.UpdateAsync(userAddResult.Result);

            RVerifyQueueItem[] failedQueue =
                isEmail
                ?
                await _context.VerifyQueueItems.Where(i => i.Email == email && i.Secret != secret && i.QueueType == RVerifyQueueType.SignUp).ToArrayAsync()
                :
                await _context.VerifyQueueItems.Where(i => i.PhoneNumber == email && i.Secret != secret && i.QueueType == RVerifyQueueType.SignUp).ToArrayAsync();
            if (failedQueue.Length != 0)
            {
                _context.VerifyQueueItems.RemoveRange(failedQueue);
            }

            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);

        }


        /// <summary>
        /// Start forgot password process using email or phone number (sms otp). If <paramref
        /// name="email"/> contains an "@" it is treated as an email address, otherwise as a
        /// phone number - same convention as <see cref="SignUp"/>. A successful
        /// <see cref="ResetPassword"/> using the resulting secret also marks the relevant
        /// channel (EmailConfirmed/PhoneNumberConfirmed) as verified, so this doubles as the
        /// recovery path for an account created while unverified signup was allowed.
        /// </summary>
        /// <param name="email">email address or phone number</param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> ForgotPassword(string email, string clientIPAddress, string clientAppName, string language)
        {
            if (string.IsNullOrEmpty(clientIPAddress))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "client ip address is empty");
            }

            if (string.IsNullOrEmpty(clientAppName))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "client app name is empty");
            }

            if (string.IsNullOrEmpty(email))
            {
                return new RServiceResult<RVerifyQueueItem>(null, "email/phone number is empty");
            }

            bool isEmail = email.Contains('@');

            RAppUser rAppUser =
                isEmail
                ?
                await _userManager.FindByEmailAsync(email)
                :
                await _userManager.Users.Where(u => u.PhoneNumber == email).SingleOrDefaultAsync();
            if (rAppUser == null)
            {
                return new RServiceResult<RVerifyQueueItem>(null, "کاربر مورد نظر یافت نشد");
            }

            if (!isEmail)
            {
                //sms costs money (unlike email), so we enforce a resend cooldown for the phone case
                RVerifyQueueItem lastAttempt =
                    await _context.VerifyQueueItems
                    .Where(i => i.QueueType == RVerifyQueueType.ForgotPassword && i.PhoneNumber == email)
                    .OrderByDescending(i => i.DateTime)
                    .FirstOrDefaultAsync();
                if (lastAttempt != null && lastAttempt.DateTime > DateTime.Now.AddSeconds(-PhoneSignUpResendCooldownSeconds))
                {
                    double secondsLeft = (lastAttempt.DateTime.AddSeconds(PhoneSignUpResendCooldownSeconds) - DateTime.Now).TotalSeconds;
                    return new RServiceResult<RVerifyQueueItem>(null, $"لطفاً {Math.Ceiling(secondsLeft)} ثانیهٔ دیگر مجدداً تلاش کنید.");
                }
            }

            var oldSecrets = await _context.VerifyQueueItems.Where(i => i.DateTime < DateTime.Now.AddDays(-1)).ToListAsync();
            if (oldSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(oldSecrets);
                await _context.SaveChangesAsync();
            }

            //checking this queue for previous signup attempts is unnecessary and is not done intentionally
            RVerifyQueueItem item = new RVerifyQueueItem()
            {
                QueueType = RVerifyQueueType.ForgotPassword,
                Email = isEmail ? email : null,
                PhoneNumber = isEmail ? null : email,
                DateTime = DateTime.Now,
                ClientIPAddress = clientIPAddress,
                ClientAppName = clientAppName,
                Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                Language = language
            };

            var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
            if (existingSecrets.Count > 0)
            {
                _context.VerifyQueueItems.RemoveRange(existingSecrets);
                await _context.SaveChangesAsync();
            }



            await _context.VerifyQueueItems.AddAsync
                (
                item
                );
            await _context.SaveChangesAsync();
            return new RServiceResult<RVerifyQueueItem>(item);
        }

        /// <summary>
        /// reset password using email or phone number (sms otp) - same convention as
        /// <see cref="ForgotPassword"/>. On success, also marks the relevant channel
        /// (EmailConfirmed/PhoneNumberConfirmed) as verified: successfully receiving and
        /// submitting this OTP is exactly the same proof of ownership used everywhere else in
        /// this file (SignUp, ChangeContact), so this is a legitimate verification, not just a
        /// password change.
        /// </summary>
        /// <param name="email">email address or phone number</param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="clientIPAddress"></param>       
        /// <returns></returns>
        public virtual async Task<RServiceResult<bool>> ResetPassword(string email, string secret, string password, string clientIPAddress)
        {
            if (bool.Parse(Configuration["AuditNetEnabled"]))
            {
                //we ignore input model in automatic auditing to prevent logging password data, so we would add a manual auditing to have enough data on login intrusion and ...
                REvent log = new REvent()
                {
                    EventType = "AppUser/ResetPassword (POST)(Manual)",
                    StartDate = DateTime.UtcNow,
                    UserName = email,
                    IpAddress = clientIPAddress
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            bool isEmail = !string.IsNullOrEmpty(email) && email.Contains('@');

            RAppUser existingUser =
                isEmail
                ?
                await _userManager.FindByEmailAsync(email)
                :
                await _userManager.Users.Where(u => u.PhoneNumber == email).SingleOrDefaultAsync();
            if (existingUser == null)
            {
                return new RServiceResult<bool>(false, isEmail ? "کاربر مورد نظر با این آدرس ایمیل یافت نشد" : "کاربر مورد نظر با این شماره تلفن یافت نشد");
            }


            if (
                email
                !=
                (await RetrieveEmailFromQueueSecret(RVerifyQueueType.ForgotPassword, secret)).Result
             )
            {
                return new RServiceResult<bool>(false, "کلمه عبور اشتباه وارد شده است");
            }

            foreach (var passwordValidator in _userManager.PasswordValidators)
            {
                var resPass = await passwordValidator.ValidateAsync(_userManager, existingUser, password);
                if (!resPass.Succeeded)
                {
                    return new RServiceResult<bool>(false, ErrorsToString(resPass.Errors));
                }
            }

            existingUser.PasswordHash = _userManager.PasswordHasher.HashPassword(existingUser, password);

            //successfully proving control of this exact channel via OTP is exactly the same
            //proof used to confirm it anywhere else (SignUp, ChangeContact) - so this also
            //verifies the account, which is what lets a user created while unverified signup
            //was allowed regain (now-gated) login access once that setting is turned back off
            if (isEmail)
            {
                existingUser.EmailConfirmed = true;
            }
            else
            {
                existingUser.PhoneNumberConfirmed = true;
            }

            await _userManager.UpdateAsync(existingUser);

            RVerifyQueueItem[] failedQueue =
                isEmail
                ?
                await _context.VerifyQueueItems.Where(i => i.Email == email && i.QueueType == RVerifyQueueType.ForgotPassword).ToArrayAsync()
                :
                await _context.VerifyQueueItems.Where(i => i.PhoneNumber == email && i.QueueType == RVerifyQueueType.ForgotPassword).ToArrayAsync();
            if (failedQueue.Length != 0)
            {
                _context.VerifyQueueItems.RemoveRange(failedQueue);
            }



            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// start changing (or first-time linking) the logged on user's email or phone number.
        /// If <paramref name="newContact"/> contains an "@" it is treated as an email address
        /// (verification code sent by email via IEmailSender at the controller level), otherwise
        /// as a phone number (verification code sent by sms via ISmsSender at the controller
        /// level).
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newContact">new email address or phone number</param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<RVerifyQueueItem>> RequestChangeContact(Guid userId, string newContact, string clientIPAddress, string clientAppName, string language)
        {
            try
            {
                if (string.IsNullOrEmpty(clientIPAddress))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client ip address is empty");
                }

                if (string.IsNullOrEmpty(clientAppName))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "client app name is empty");
                }

                if (string.IsNullOrEmpty(newContact))
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "email/phone number is empty");
                }

                bool isEmail = newContact.Contains('@');

                RAppUser existingUser =
                    isEmail
                    ?
                    await _userManager.FindByEmailAsync(newContact)
                    :
                    await _userManager.Users.Where(u => u.PhoneNumber == newContact).SingleOrDefaultAsync();
                if (existingUser != null && existingUser.Id != userId)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, isEmail ? $"کاربری با این ایمیل وجود دارد. - {newContact}" : $"کاربری با این شماره تلفن وجود دارد. - {newContact}");
                }

                RAppUser requestingUser = await _userManager.FindByIdAsync(userId.ToString());
                if (requestingUser == null)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "user == null");
                }

                if (isEmail && newContact == requestingUser.Email)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "این آدرس ایمیل هم اکنون ثبت شده است.");
                }
                if (!isEmail && newContact == requestingUser.PhoneNumber)
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "این شماره تلفن هم اکنون ثبت شده است.");
                }

                if (!isEmail)
                {
                    //sms costs money (unlike email), so we enforce a resend cooldown for the phone case
                    RVerifyQueueItem lastAttempt =
                        await _context.VerifyQueueItems
                        .Where(i => i.QueueType == RVerifyQueueType.ChangeContact && i.PhoneNumber == newContact)
                        .OrderByDescending(i => i.DateTime)
                        .FirstOrDefaultAsync();
                    if (lastAttempt != null && lastAttempt.DateTime > DateTime.Now.AddSeconds(-PhoneSignUpResendCooldownSeconds))
                    {
                        double secondsLeft = (lastAttempt.DateTime.AddSeconds(PhoneSignUpResendCooldownSeconds) - DateTime.Now).TotalSeconds;
                        return new RServiceResult<RVerifyQueueItem>(null, $"لطفاً {Math.Ceiling(secondsLeft)} ثانیهٔ دیگر مجدداً تلاش کنید.");
                    }
                }

                var oldSecrets = await _context.VerifyQueueItems.Where(i => i.DateTime < DateTime.Now.AddDays(-1)).ToListAsync();
                if (oldSecrets.Count > 0)
                {
                    _context.VerifyQueueItems.RemoveRange(oldSecrets);
                    await _context.SaveChangesAsync();
                }

                //checking this queue for previous change-contact attempts is unnecessary and is not done intentionally
                RVerifyQueueItem item = new RVerifyQueueItem()
                {
                    QueueType = RVerifyQueueType.ChangeContact,
                    Email = isEmail ? newContact : null,
                    PhoneNumber = isEmail ? null : newContact,
                    DateTime = DateTime.Now,
                    ClientIPAddress = clientIPAddress,
                    ClientAppName = clientAppName,
                    Secret = $"{(new Random(DateTime.Now.Millisecond)).Next(0, 99999)}".PadLeft(6, '0'),
                    Language = language
                };

                var existingSecrets = await _context.VerifyQueueItems.Where(i => i.Secret == item.Secret).ToListAsync();
                if (existingSecrets.Count > 0)
                {
                    _context.VerifyQueueItems.RemoveRange(existingSecrets);
                    await _context.SaveChangesAsync();
                }

                await _context.VerifyQueueItems.AddAsync
                    (
                    item
                    );
                await _context.SaveChangesAsync();
                return new RServiceResult<RVerifyQueueItem>(item);

            }
            catch (Exception exp)
            {
                return new RServiceResult<RVerifyQueueItem>(null, exp.ToString());
            }
        }

        /// <summary>
        /// confirm a pending email/phone number change (or first-time link) started by
        /// <see cref="RequestChangeContact"/>, using the OTP secret
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="secret"></param>
        /// <param name="clientIPAddress"></param>
        /// <returns>old value (null if this was a first-time link) + new value</returns>
        public virtual async Task<RServiceResult<ContactChangeResult>> ChangeContact(Guid userId, string secret, string clientIPAddress)
        {
            try
            {
                RAppUser updatingUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (updatingUserInfo == null)
                {
                    return new RServiceResult<ContactChangeResult>(null, "user == null");
                }

                secret = (secret ?? "").Trim();
                RVerifyQueueItem queueItem = await _context.VerifyQueueItems.Where(i => i.QueueType == RVerifyQueueType.ChangeContact && i.Secret == secret).SingleOrDefaultAsync();
                if (queueItem == null)
                {
                    return new RServiceResult<ContactChangeResult>(null, "کد وارد شده معتبر نیست یا منقضی شده است.");
                }

                bool isEmail = !string.IsNullOrEmpty(queueItem.Email);
                string newContact = isEmail ? queueItem.Email : queueItem.PhoneNumber;

                if (bool.Parse(Configuration["AuditNetEnabled"]))
                {
                    //we ignore input model in automatic auditing to prevent logging password data, so we would add a manual auditing to have enough data on login intrusion and ...
                    REvent log = new REvent()
                    {
                        EventType = $"Change Contact (POST)(Manual) to {newContact}",
                        StartDate = DateTime.UtcNow,
                        UserName = updatingUserInfo.UserName,
                        IpAddress = clientIPAddress
                    };
                    _context.AuditLogs.Add(log);
                    await _context.SaveChangesAsync();
                }

                {
                    //re-check uniqueness: time has passed since RequestChangeContact, so someone
                    //else could have taken this email/phone number in the meantime
                    RAppUser notShouldExistUser =
                        isEmail
                        ?
                        await _userManager.FindByEmailAsync(newContact)
                        :
                        await _userManager.Users.Where(u => u.PhoneNumber == newContact).SingleOrDefaultAsync();
                    if (notShouldExistUser != null && notShouldExistUser.Id != userId)
                    {
                        return new RServiceResult<ContactChangeResult>(null, isEmail ? $"کاربری با این ایمیل وجود دارد. - {newContact}" : $"کاربری با این شماره تلفن وجود دارد. - {newContact}");
                    }
                }

                string oldValue = isEmail ? updatingUserInfo.Email : updatingUserInfo.PhoneNumber;

                if (!string.IsNullOrEmpty(oldValue))
                {
                    //this is a change, not a first-time link: keep an audit trail; the caller is
                    //expected to notify the OLD contact value (ContactChanged) as a security notice
                    _context.UserOldContacts.Add
                        (
                        new UserOldContact()
                        {
                            Id = Guid.NewGuid(),
                            UserId = updatingUserInfo.Id,
                            IsEmail = isEmail,
                            Value = oldValue,
                            NormalizedValue = isEmail ? _userManager.NormalizeEmail(oldValue) : null,
                            ChangeDate = DateTime.Now,
                        }
                        );
                }

                if (isEmail)
                {
                    if (updatingUserInfo.UserName == updatingUserInfo.Email)
                    {
                        updatingUserInfo.UserName = newContact;
                    }
                    updatingUserInfo.Email = newContact;
                    updatingUserInfo.NormalizedEmail = _userManager.NormalizeEmail(newContact);
                    updatingUserInfo.EmailConfirmed = true;
                }
                else
                {
                    if (updatingUserInfo.UserName == updatingUserInfo.PhoneNumber)
                    {
                        updatingUserInfo.UserName = newContact;
                    }
                    updatingUserInfo.PhoneNumber = newContact;
                    updatingUserInfo.PhoneNumberConfirmed = true;
                }

                await _userManager.UpdateAsync(updatingUserInfo);

                RVerifyQueueItem[] failedQueue =
                    isEmail
                    ?
                    await _context.VerifyQueueItems.Where(i => i.Email == newContact && i.QueueType == RVerifyQueueType.ChangeContact).ToArrayAsync()
                    :
                    await _context.VerifyQueueItems.Where(i => i.PhoneNumber == newContact && i.QueueType == RVerifyQueueType.ChangeContact).ToArrayAsync();
                if (failedQueue.Length != 0)
                {
                    _context.VerifyQueueItems.RemoveRange(failedQueue);
                }

                await _context.SaveChangesAsync();

                return new RServiceResult<ContactChangeResult>(
                    new ContactChangeResult()
                    {
                        IsEmail = isEmail,
                        OldValue = string.IsNullOrEmpty(oldValue) ? null : oldValue,
                        NewValue = newContact,
                    });

            }
            catch (Exception exp)
            {
                return new RServiceResult<ContactChangeResult>(null, exp.ToString());
            }

        }

        /// <summary>
        /// Find User By Email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<RServiceResult<PublicRAppUser>> FindUserByEmail(string email)
        {
            RAppUser appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
                return new RServiceResult<PublicRAppUser>(null);
            return new RServiceResult<PublicRAppUser>(
                new PublicRAppUser()
                {
                    Id = appUser.Id,
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    FirstName = appUser.FirstName,
                    SurName = appUser.SurName,
                    PhoneNumber = appUser.PhoneNumber,
                    RImageId = appUser.RImageId,
                    Status = appUser.Status,
                    NickName = appUser.NickName,
                    Website = appUser.Website,
                    Bio = appUser.Bio,
                    EmailConfirmed = appUser.EmailConfirmed,
                    CreateDate = appUser.CreateDate,
                });

        }


        /// <summary>
        /// delete tenant
        /// </summary>
        /// <returns></returns>
        public RServiceResult<bool> DeleteTenant()
        {
            _context.DeleteDb();
            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// EnsureDefaultUserExists
        /// </summary>
        /// <returns></returns>
        private async Task<RServiceResult<bool>> EnsureDefaultUserExists()
        {

            //If no user exists create default one                
            if (_userManager.Users.Count() == 0)
            {
                RAppUser admin = new RAppUser()
                {
                    UserName = $"{Configuration.GetSection("RSecurityBackend")["FirstUserEmail"]}",
                    FirstName = "راهبر",
                    SurName = "سیستم",
                    Email = $"{Configuration.GetSection("RSecurityBackend")["FirstUserEmail"]}",
                    EmailConfirmed = true,
                    PhoneNumber = "00989123456789",
                    PhoneNumberConfirmed = true,
                    CreateDate = DateTime.Now,
                    Status = RAppUserStatus.Active
                };

                var identityResult = await _userManager.CreateAsync(
                    admin, "Test!123"
                    );
                if (!identityResult.Succeeded)
                {
                    return new RServiceResult<bool>(false, "Error creating default user : " + ErrorsToString(identityResult.Errors));
                }

                if (!await _roleManager.RoleExistsAsync(_userRoleService.AdministratorRoleName))
                {
                    identityResult = await _roleManager.CreateAsync(new RAppRole(_userRoleService.AdministratorRoleName));
                    if (!identityResult.Succeeded)
                    {
                        return new RServiceResult<bool>(false, "Error creating Administrator role : " + ErrorsToString(identityResult.Errors));
                    }
                }

                identityResult = await _userManager.AddToRoleAsync(admin, _userRoleService.AdministratorRoleName);
                if (!identityResult.Succeeded)
                {
                    return new RServiceResult<bool>(false, "Error adding admin to Administrator role : " + ErrorsToString(identityResult.Errors));
                }
            }
            return new RServiceResult<bool>(true);

        }





        /// <summary>
        /// secret used for generating Jwt token
        /// </summary>
        public string TokenSecret { get { return $"{Configuration.GetSection("Security")["Secret"]}"; } }

        /// <summary>
        /// JWT Tokens Expiration Time Out
        /// </summary>
        public int DefaultTokenExpirationInSeconds { get { return int.Parse($"{Configuration.GetSection("Security")["DefaultTokenExpirationInSeconds"]}"); } }



        #region Internals

        #region Token Generation

        /// <summary>
        /// Token Generation
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private async Task<RServiceResult<string>> GenerateToken(string username, Guid userId, Guid sessionId, string language)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new RServiceResult<string>(null, "کاربر مورد نظر یافت نشد");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim("UserId", userId.ToString()),
                new Claim("SessionId", sessionId.ToString()),
                new Claim("Language", language)
            };

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (Claim roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: $"{Configuration.GetSection("RSecurityBackend")["ApplicationName"]}",
                audience: "Everyone",
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DefaultTokenExpirationInSeconds < 0 ? null : DateTime.UtcNow.AddSeconds(DefaultTokenExpirationInSeconds),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSecret)), SecurityAlgorithms.HmacSha256)
                );

            return new RServiceResult<string>(new JwtSecurityTokenHandler().WriteToken(token));

        }


        /// <summary>
        /// Extract Information From Token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="username"></param>
        /// <param name="userId"></param>
        /// <param name="sessionId"></param>
        public void ExtractTokenInfo(string token, out string username, out Guid userId, out Guid sessionId)
        {

            var principal = GetPrincipalFromToken(token, true);
            username = principal.Identity.Name;
            userId = new Guid(principal.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            sessionId = new Guid(principal.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
        }

        /// <summary>
        /// get principal for token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="expired"></param>
        /// <returns></returns>
        public ClaimsPrincipal GetPrincipalFromToken(string token, bool expired)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidAudience = "Everyone",
                ValidateIssuer = true,
                ValidIssuer = $"{Configuration.GetSection("RSecurityBackend")["ApplicationName"]}",

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenSecret)),

                ValidateLifetime = !expired, //important

                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        #endregion

        #region Identity Errors conversion
        /// <summary>
        /// convert identity errors to string
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        protected static string ErrorsToString(IEnumerable<IdentityError> errors)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IdentityError error in errors)
            {
                sb.AppendLine(error.Description);
            }
            return sb.ToString();
        }
        #endregion
        #endregion


        #region Signup/Forget password email related overridables
        /// <summary>
        /// Sign Up/forget passsword/delete Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        public virtual string GetEmailSubject(RVerifyQueueType op, string secretCode)
        {
            if (op == RVerifyQueueType.ContactChanged)
                return "Email changed";

            string opString = op == RVerifyQueueType.SignUp ? "SignUp" : op == RVerifyQueueType.ForgotPassword ? "Forgot Password" : op == RVerifyQueueType.KickOutUser ? "User Removal" : op == RVerifyQueueType.ChangeContact ? "Change Email" : "Self Delete User";
            return $"Application {opString} {(op == RVerifyQueueType.KickOutUser ? "Cause" : "Code")}:{secretCode}";

        }

        /// <summary>
        /// Sign Up/forget passsword/delete Email Html Content
        /// </summary>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        /// <param name="signupCallbackUrl"></param>
        /// <returns>html content</returns>
        public virtual string GetEmailHtmlContent(RVerifyQueueType op, string secretCode, string signupCallbackUrl)
        {
            if (!string.IsNullOrEmpty(signupCallbackUrl))
                return $"{signupCallbackUrl}?secret={secretCode}";
            if (op == RVerifyQueueType.ContactChanged)
                return $"ایمیل حساب کاربری شما به {secretCode} تغییر کرد. اگر این درخواست از طرف شما نبوده لطفاً هر چه سریع‌تر با پشتیبانی تماس بگیرید.";
            string opString = op == RVerifyQueueType.SignUp ? "ثبت نام" : op == RVerifyQueueType.ForgotPassword ? "فراموشی رمز" : op == RVerifyQueueType.UserSelfDelete ? "حذف کاربر" : "تغییر ایمیل";
            return op == RVerifyQueueType.KickOutUser ? $"حساب کاربری شما به دلیل {secretCode} حذف شد." : $"لطفا {secretCode} را در صفحهٔ {opString} وارد کنید.";
        }

        /// <summary>
        /// Sign Up By Phone Sms Text (override to customize wording/branding)
        /// </summary>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        /// <returns>sms text</returns>
        public virtual string GetSmsText(RVerifyQueueType op, string secretCode)
        {
            if (op == RVerifyQueueType.ContactChanged)
                return $"شماره تلفن حساب کاربری شما به {secretCode} تغییر کرد. اگر این درخواست از طرف شما نبوده لطفاً هر چه سریع‌تر با پشتیبانی تماس بگیرید.";
            return $"کد تایید شما: {secretCode}";
        }

        /// <summary>
        /// minimum seconds to wait between two consecutive sms otp requests for the same phone number
        /// (override to read from configuration if you want it tunable without a code change)
        /// </summary>
        public virtual int PhoneSignUpResendCooldownSeconds => 60;

        /// <summary>
        /// if true, <see cref="FinalizeSignUp"/> creates an EMAIL-signed-up account without
        /// requiring the OTP secret to have actually been delivered (EmailConfirmed is left
        /// false instead) - an emergency escape hatch for when outbound email delivery is
        /// broken. Read from configuration ("SignUp:AllowUnverified"), defaults to false.
        /// While this is true, <see cref="Login"/> allows logging into such accounts; while
        /// false, an already-existing unverified account can only regain access (and get
        /// EmailConfirmed set) by completing <see cref="ForgotPassword"/>/<see cref="ResetPassword"/>.
        /// </summary>
        public virtual bool AllowUnverifiedEmailSignUp
        {
            get
            {
                string allow = Configuration.GetSection("SignUp")["AllowUnverified"];
                return !string.IsNullOrEmpty(allow) && bool.Parse(allow);
            }
        }

        /// <summary>
        /// same as <see cref="AllowUnverifiedEmailSignUp"/>, for the phone/sms signup channel.
        /// Read from configuration ("PhoneSignUp:AllowUnverified"), defaults to false.
        /// </summary>
        public virtual bool AllowUnverifiedPhoneSignUp
        {
            get
            {
                string allow = Configuration.GetSection("PhoneSignUp")["AllowUnverified"];
                return !string.IsNullOrEmpty(allow) && bool.Parse(allow);
            }
        }

        #endregion

        #region Users' Bad Behaviour Management
        /// <summary>
        /// log user bad behaviuor
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBehaviourLog>> LogUserBehaviourAsync(Guid userId, string description)
        {
            RUserBehaviourLog log = new RUserBehaviourLog()
            {
                UserId = userId,
                DateTime = DateTime.Now,
                Description = description,
            };
            _context.UserBehaviourLogs.Add(log);
            await _context.SaveChangesAsync();
            return new RServiceResult<RUserBehaviourLog>(log);
        }

        /// <summary>
        /// get user behaviour logs
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserBehaviourLog[]>> GetUserBehaviourLogsAsync(Guid userId)
        {
            return new RServiceResult<RUserBehaviourLog[]>
                (
                await _context.UserBehaviourLogs.Where(b => b.UserId == userId).OrderByDescending(b => b.DateTime).ToArrayAsync()
                );
        }

        /// <summary>
        /// lockout a user for a period
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cause"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> LockoutAsync(Guid userId, string cause, DateTimeOffset offset)
        {
            RAppUser appUser =
                await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();
            var res = await _userManager.SetLockoutEnabledAsync(appUser, true);
            if (!res.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(res.Errors));
            }
            res = await _userManager.SetLockoutEndDateAsync(appUser, offset);
            if (!res.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(res.Errors));
            }
            _context.Sessions.RemoveRange(await _context.Sessions.Where(u => u.RAppUserId == userId).ToArrayAsync());
            await _context.SaveChangesAsync();

            appUser.LockoutMessage = cause;
            res = await _userManager.UpdateAsync(appUser);
            if (!res.Succeeded)
            {
                return new RServiceResult<bool>(false, ErrorsToString(res.Errors));
            }
            return new RServiceResult<bool>(true);

        }

        /// <summary>
        /// before kicking out a bad behving user ban him or her from signing up again.
        /// Bans not only the user's CURRENT email/phone number, but every email and phone
        /// number this user has ever verified and later moved away from via
        /// <see cref="ChangeContact"/> (see <see cref="UserOldContact"/>) - otherwise a user
        /// could dodge a ban simply by changing their contact info right before being kicked
        /// out, then signing up again with the address they just abandoned.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cause">document the cause</param>
        /// <returns>
        /// one representative banned entry (current email, else current phone, else the first
        /// historical value banned) for backward compatibility with existing callers - every
        /// value banned by this call is persisted regardless of what is returned here
        /// </returns>
        public async Task<RServiceResult<BannedEmail>> BanUserFromSigningUpAgainAsync(Guid userId, string cause)
        {
            RAppUser appUser =
                await _userManager.Users.Where(u => u.Id == userId).SingleOrDefaultAsync();

            string NormalizeGmailAlias(string normalizedEmail)
            {
                if (string.IsNullOrEmpty(normalizedEmail))
                    return normalizedEmail;
                if (normalizedEmail.Contains("@GMAIL.COM"))
                {
                    if (normalizedEmail.Contains("+") && normalizedEmail.IndexOf("+") < normalizedEmail.IndexOf("@GMAIL.COM"))
                    {
                        normalizedEmail = normalizedEmail.Substring(0, normalizedEmail.IndexOf("+")) + "@GMAIL.COM";
                    }
                }
                return normalizedEmail;
            }

            //ordered so the CURRENT values come first - preserved for the single-entry return
            //value below, to keep existing callers seeing the same thing they used to
            List<string> orderedEmailsToBan = new List<string>();
            List<string> orderedPhonesToBan = new List<string>();

            if (!string.IsNullOrEmpty(appUser.NormalizedEmail))
            {
                orderedEmailsToBan.Add(NormalizeGmailAlias(appUser.NormalizedEmail));
            }
            if (!string.IsNullOrEmpty(appUser.PhoneNumber))
            {
                orderedPhonesToBan.Add(appUser.PhoneNumber);
            }

            List<UserOldContact> history = await _context.UserOldContacts.Where(c => c.UserId == userId).ToListAsync();
            foreach (UserOldContact old in history)
            {
                if (string.IsNullOrEmpty(old.Value))
                    continue;
                if (old.IsEmail)
                {
                    string normalized = string.IsNullOrEmpty(old.NormalizedValue) ? _userManager.NormalizeEmail(old.Value) : old.NormalizedValue;
                    orderedEmailsToBan.Add(NormalizeGmailAlias(normalized));
                }
                else
                {
                    orderedPhonesToBan.Add(old.Value);
                }
            }

            //de-duplicate while preserving order (a value could repeat across history, or a
            //historical value could coincidentally match the current one)
            List<string> emailsToBan = orderedEmailsToBan.Distinct().ToList();
            List<string> phonesToBan = orderedPhonesToBan.Distinct().ToList();

            //skip anything already banned - keeps this idempotent if ever called more than
            //once for the same user, and avoids duplicate rows
            List<BannedEmail> alreadyBanned =
                await _context.BannedEmails
                .Where(b => (b.NormalizedEmail != null && emailsToBan.Contains(b.NormalizedEmail)) || (b.PhoneNumber != null && phonesToBan.Contains(b.PhoneNumber)))
                .ToListAsync();
            HashSet<string> alreadyBannedEmails = alreadyBanned.Where(b => b.NormalizedEmail != null).Select(b => b.NormalizedEmail).ToHashSet();
            HashSet<string> alreadyBannedPhones = alreadyBanned.Where(b => b.PhoneNumber != null).Select(b => b.PhoneNumber).ToHashSet();

            BannedEmail primaryBannedEntry = null;

            foreach (string normalizedEmail in emailsToBan)
            {
                if (alreadyBannedEmails.Contains(normalizedEmail))
                    continue;
                BannedEmail entry = new BannedEmail()
                {
                    NormalizedEmail = normalizedEmail,
                    PhoneNumber = null,
                    Description = cause
                };
                _context.BannedEmails.Add(entry);
                if (primaryBannedEntry == null)
                    primaryBannedEntry = entry;
            }

            foreach (string phoneNumber in phonesToBan)
            {
                if (alreadyBannedPhones.Contains(phoneNumber))
                    continue;
                BannedEmail entry = new BannedEmail()
                {
                    NormalizedEmail = null,
                    PhoneNumber = phoneNumber,
                    Description = cause
                };
                _context.BannedEmails.Add(entry);
                if (primaryBannedEntry == null)
                    primaryBannedEntry = entry;
            }

            if (primaryBannedEntry == null)
            {
                //nothing new to ban: either the user had no email/phone at all (current or
                //historical), or every value they ever held is already banned
                BannedEmail existingRepresentative =
                    alreadyBanned.FirstOrDefault(b => (!string.IsNullOrEmpty(b.NormalizedEmail) && emailsToBan.Contains(b.NormalizedEmail)) || (!string.IsNullOrEmpty(b.PhoneNumber) && phonesToBan.Contains(b.PhoneNumber)));
                if (existingRepresentative != null)
                {
                    return new RServiceResult<BannedEmail>(existingRepresentative);
                }
                return new RServiceResult<BannedEmail>(null, "کاربر هیچ ایمیل یا شماره تلفنی (فعلی یا سابق) برای مسدود کردن ندارد.");
            }

            await _context.SaveChangesAsync();
            return new RServiceResult<BannedEmail>(primaryBannedEntry);

        }

        /// <summary>
        /// get banned email information
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<RServiceResult<BannedEmail>> GetBannedEmailInformationAsync(string email)
        {
            email = _userManager.NormalizeEmail(email);
            if (email.Contains("@GMAIL.COM"))
            {
                if (email.Contains("+") && email.IndexOf("+") < email.IndexOf("@GMAIL.COM"))
                {
                    email = email.Substring(0, email.IndexOf("+")) + "@GMAIL.COM";
                }
            }
            return new RServiceResult<BannedEmail>(await _context.BannedEmails.Where(b => b.NormalizedEmail == email).FirstOrDefaultAsync());
        }

        /// <summary>
        /// get banned phone number information
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public async Task<RServiceResult<BannedEmail>> GetBannedPhoneNumberInformationAsync(string phoneNumber)
        {
            return new RServiceResult<BannedEmail>(await _context.BannedEmails.Where(b => b.PhoneNumber == phoneNumber).FirstOrDefaultAsync());
        }
        #endregion

        /// <summary>
        /// notify all users
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="htmlText"></param>
        /// <param name="notificationType"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<RServiceResult<bool>> NotifyAllUsersAsync(string subject, string htmlText, NotificationType notificationType = NotificationType.NoActionRequired, bool email = false)
        {
            try
            {
                var users = _userManager.Users;
                foreach (var user in users)
                {
                    RUserNotification notification =
                    new RUserNotification()
                    {
                        UserId = user.Id,
                        DateTime = DateTime.Now,
                        Status = NotificationStatus.Unread,
                        Subject = subject,
                        HtmlText = htmlText,
                        NotificationType = notificationType,
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();
                if (email)
                {
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            //await is not called by purpose
                            _ = _emailSender.SendEmailAsync(user.Email, subject, htmlText);
                        }
                    }
                }
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }


        /// <summary>
        /// Main Database context
        /// </summary>
        protected readonly RSecurityDbContext<RAppUser, RAppRole, Guid> _context;

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;
        /// <summary>
        /// User Role Service
        /// </summary>
        protected readonly IUserRoleService _userRoleService;
        /// <summary>
        /// Identity User Manageer
        /// </summary>
        protected UserManager<RAppUser> _userManager = null;
        /// <summary>
        /// Identity SignIn Manager
        /// </summary>
        protected SignInManager<RAppUser> _signInManager = null;
        /// <summary>
        /// Identity Role Manager
        /// </summary>
        protected RoleManager<RAppRole> _roleManager = null;

        /// <summary>
        /// secret generator
        /// </summary>
        protected readonly ISecretGenerator _secretGenerator;

        /// <summary>
        /// Email sender
        /// </summary>
        protected readonly IEmailSender _emailSender;


        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="secretGenerator"></param>
        /// <param name="imageFileService"></param>
        /// <param name="userRoleService"></param>
        /// <param name="configuration"></param>
        /// <param name="emailSender"></param>
        public AppUserService(
            RSecurityDbContext<RAppUser, RAppRole, Guid> context,
            UserManager<RAppUser> userManager,
            SignInManager<RAppUser> signInManager,
            RoleManager<RAppRole> roleManager,
            ISecretGenerator secretGenerator,
            IImageFileService imageFileService,
            IUserRoleService userRoleService,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _secretGenerator = secretGenerator;
            _imageFileService = imageFileService;
            _userRoleService = userRoleService;
            Configuration = configuration;
            _emailSender = emailSender;
        }
    }
}
