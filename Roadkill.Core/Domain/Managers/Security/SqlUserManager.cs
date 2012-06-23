﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Web.Configuration;
using System.Configuration;
using System.Data.SqlClient;
using System.Configuration.Provider;
using NHibernate;

namespace Roadkill.Core
{
	/// <summary>
	/// Provides user management using the Roadkill database (via NHibernate).
	/// </summary>
	public class SqlUserManager : UserManager
	{
		/// <summary>
		/// Indicates whether this UserManager can perform deletes, updates or inserts for users.
		/// </summary>
		public override bool IsReadonly
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Activates the user with given activation key
		/// </summary>
		/// <param name="activationKey">The randomly generated activation key for the user.</param>
		/// <returns>True if the activation was successful</returns>
		public override bool ActivateUser(string activationKey)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.ActivationKey == activationKey && u.IsActivated == false);
				if (user != null)
				{
					user.IsActivated = true;
					NHibernateRepository.Current.SaveOrUpdate<User>(user);

					return true;
				}
				else
				{
					return false;
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred while activating the user with key {0}", activationKey);
			}
		}

		/// <summary>
		/// Adds a user to the system, and sets the <see cref="User.IsActivated"/> to true.
		/// </summary>
		/// <param name="email">The email or username.</param>
		/// <param name="password">The password.</param>
		/// <param name="isAdmin">if set to <c>true</c> the user is added as an admin.</param>
		/// <param name="isEditor">if set to <c>true</c> the user is added as an editor.</param>
		/// <returns>
		/// true if the user was added; false if the user already exists.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while adding the new user.</exception>
		public override bool AddUser(string email, string username, string password, bool isAdmin, bool isEditor)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email || u.Username == username);
				if (user == null)
				{
					user = new User();
					user.Email = email;
					user.Username = username;
					user.SetPassword(password);
					user.IsAdmin = isAdmin;
					user.IsEditor = isEditor;
					user.IsActivated = true;
					NHibernateRepository.Current.SaveOrUpdate<User>(user);

					return true;
				}
				else
				{
					return false;
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred while adding the new user {0}", email);
			}
		}

		/// <summary>
		/// Authenticates the user with the specified email.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <param name="password">The password.</param>
		/// <returns>
		/// true if the authentication was sucessful;false otherwise.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while authenticating the user.</exception>
		public override bool Authenticate(string email, string password)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email && u.IsActivated);
				if (user != null)
				{
					if (user.Password == User.HashPassword(password, user.Salt))
						return true;
				}

				return false;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred authentication user {0}", email);
			}
		}

		/// <summary>
		/// Changes the password of a user with the given email.
		/// </summary>
		/// <param name="email">The email address of the user.</param>
		/// <param name="newPassword">The new password.</param>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while changing the password OR the password is empty.</exception>
		public override void ChangePassword(string email, string newPassword)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(newPassword))
					throw new SecurityException(null, "Cannot change the password as it's empty.");

				User user = Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					user.Salt = new Salt();
					user.SetPassword(newPassword);
					NHibernateRepository.Current.SaveOrUpdate<User>(user);
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred changing the password for {0}", email);
			}
		}

		/// <summary>
		/// Changes the password of the user with the given email, authenticating their password first..
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <param name="oldPassword">The old password.</param>
		/// <param name="newPassword">The new password to change to.</param>
		/// <returns>
		/// true if the password change was successful;false if the previous password was incorrect.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while changing the password OR the new password is empty.</exception>
		public override bool ChangePassword(string email, string oldPassword, string newPassword)
		{
			try
			{
				if (!Authenticate(email, oldPassword))
					return false;

				User user = Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					if (string.IsNullOrWhiteSpace(newPassword))
					{
						throw new SecurityException(null, "Changing password failed. The new password is blank.");
					}

					user.Salt = new Salt();
					user.SetPassword(newPassword);

					NHibernateRepository.Current.SaveOrUpdate<User>(user);
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred changing the password for {0}", email);
			}
		}

		/// <summary>
		/// Deletes a user with the given email from the system.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <returns>
		/// true if the deletion was successful;false if the user could not be found.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while deleting the user.</exception>
		public override bool DeleteUser(string email)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					NHibernateRepository.Current.Delete<User>(user);
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred deleting the user with the email {0}", email);
			}
		}

		/// <summary>
		/// Retrieves a full <see cref="User"/> object using the unique ID provided..
		/// </summary>
		/// <param name="email">The ID of the user.</param>
		/// <returns>A <see cref="User"/> object</returns>
		public override User GetUserById(Guid id)
		{
			return Users.FirstOrDefault(u => u.Id == id);
		}

		/// <summary>
		/// Retrieves a full <see cref="User"/> object for the email address provided, or null if the user doesn't exist.
		/// </summary>
		/// <param name="email">The email address of the user to get</param>
		/// <returns>A <see cref="User"/> object</returns>
		public override User GetUser(string email)
		{
			return Users.FirstOrDefault(u => u.Email == email);
		}

		/// <summary>
		/// Retrieves a full <see cref="User"/> object for a password reset request.
		/// </summary>
		/// <param name="resetKey"></param>
		/// <returns>A <see cref="User"/> object</returns>
		public override User GetUserByResetKey(string resetKey)
		{
			return Users.FirstOrDefault(u => u.PasswordResetKey == resetKey);
		}

		/// <summary>
		/// Determines whether the specified user with the given email is an admin.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <returns>
		/// true if the user is an admin; false otherwise.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while email/username.</exception>
		public override bool IsAdmin(string email)
		{
			try
			{
				return Users.FirstOrDefault(u => u.Email == email && u.IsAdmin) != null;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred checking if {0} is an admin", email);
			}
		}

		/// <summary>
		/// Determines whether the specified user with the given email is an editor.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <returns>
		/// true if the user is an editor; false otherwise.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while checking email/username.</exception>
		public override bool IsEditor(string email)
		{
			try
			{
				return Users.FirstOrDefault(u => u.Email == email && u.IsEditor) != null;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred checking if {0} is an editor", email);
			}
		}

		/// <summary>
		/// Lists all admins in the system.
		/// </summary>
		/// <returns>
		/// A list of email/usernames who are admins.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while getting the admins.</exception>
		public override IEnumerable<UserSummary> ListAdmins()
		{
			try
			{
				var users = Users.ToList().Where(u => u.IsAdmin).Select(u => u.ToSummary());
				return users;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred listing all the admins");
			}
		}

		/// <summary>
		/// Lists all editors in the system.
		/// </summary>
		/// <returns>
		/// A list of email/usernames who are editors.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while getting the editors.</exception>
		public override IEnumerable<UserSummary> ListEditors()
		{
			try
			{
				var users = Users.ToList().Where(u => u.IsEditor).Select(u => u.ToSummary());
				return users;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred listing all the editor");
			}
		}

		/// <summary>
		/// Signs the user out with (typically with <see cref="FormsAuthentication"/>).
		/// </summary>
		public override void Logout()
		{
			FormsAuthentication.SignOut();
		}

		/// <summary>
		/// Resets the password for the user with the given email.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <returns>
		/// The new randomly generated password
		/// </returns>
		public override string ResetPassword(string email)
		{
			if (string.IsNullOrEmpty(email))
				throw new SecurityException("The email provided to ResetPassword is null or empty.", null);

			try
			{
				User user = UserManager.Current.GetUser(email);

				if (user != null)
				{
					user.PasswordResetKey = Guid.NewGuid().ToString();
					NHibernateRepository.Current.SaveOrUpdate<User>(user);

					return user.PasswordResetKey;
				}
				else
				{
					return "";
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred with resetting the password of {0}", email);
			}
		}

		/// <summary>
		/// Creates a user in the system without setting the <see cref="User.IsActivated"/>, in other words for a user confirmation email.
		/// </summary>
		/// <param name="user">The user details to signup.</param>
		/// <param name="completed">Called once the signup (e.g. email is sent) is complete. Pass Null for no action.</param>
		/// <returns>
		/// The activation key for the signup.
		/// </returns>
		public override string Signup(UserSummary summary, Action completed)
		{
			if (summary == null)
				throw new SecurityException("The summary provided to Signup is null.", null);

			try
			{
				// Create the new user
				summary.ActivationKey = Guid.NewGuid().ToString();
				User user = new User();
				user.Username = summary.NewUsername;
				user.ActivationKey = summary.ActivationKey;
				user.Email = summary.NewEmail;
				user.Firstname = summary.Firstname;
				user.Lastname = summary.Lastname;
				user.SetPassword(summary.Password);
				user.IsEditor = true;
				user.IsAdmin = false;
				user.IsActivated = false;
				NHibernateRepository.Current.SaveOrUpdate<User>(user);

				if (completed != null)
					completed();

				return user.ActivationKey;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred with the signup of {0}", summary.NewEmail);
			}
		}

		/// <summary>
		/// Adds or remove the user with the email address as an editor.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while setting the user to an editor.</exception>
		public override void ToggleEditor(string email)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					user.IsEditor = !user.IsEditor;
					NHibernateRepository.Current.SaveOrUpdate<User>(user);
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred adding the editor {0}", email);
			}
		}

		/// <summary>
		/// Adds or remove the user with the email address as an admin.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while setting the user to an admin.</exception>
		public override void ToggleAdmin(string email)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					user.IsAdmin = !user.IsAdmin;
					NHibernateRepository.Current.SaveOrUpdate<User>(user);
				}
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred adding the admin {0}", email);
			}
		}

		/// <summary>
		/// Changes the username of a user to a new username.
		/// </summary>
		/// <param name="summary">The user details to change. The password property is ignored for this object - use ChangePassword instead.</param>
		/// <returns>
		/// true if the change was successful;false if the new username already exists in the system.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while changing the email/username.</exception>
		public override bool UpdateUser(UserSummary summary)
		{
			try
			{
				User user;

				// These checks is run in the UserSummary object by MVC - but doubled up in here for _when_ the API is used without MVC.
				if (summary.ExistingEmail != summary.NewEmail)
				{
					user = Users.FirstOrDefault(u => u.Email == summary.NewEmail);
					if (user != null)
						throw new SecurityException(null, "The email provided already exists.");
				}

				if (summary.ExistingUsername != summary.NewUsername)
				{
					user = Users.FirstOrDefault(u => u.Username == summary.NewUsername);
					if (user != null)
						throw new SecurityException(null, "The username provided already exists.");
				}

				user = Users.FirstOrDefault(u => u.Id == summary.Id);
				if (user == null)
					throw new SecurityException(null, "The user does not exist.");

				// Update the profile details
				user.Firstname = summary.Firstname;
				user.Lastname = summary.Lastname;
				NHibernateRepository.Current.SaveOrUpdate<User>(user);

				// Save the email
				if (summary.ExistingEmail != summary.NewEmail)
				{
					user = Users.FirstOrDefault(u => u.Email == summary.ExistingEmail);
					if (user != null)
					{
						user.Email = summary.NewEmail;
						NHibernateRepository.Current.SaveOrUpdate<User>(user);
					}
					else
					{
						return false;
					}
				}

				// Save the username
				if (summary.ExistingUsername != summary.NewUsername)
				{
					user = Users.FirstOrDefault(u => u.Username == summary.ExistingUsername);
					if (user != null)
					{
						user.Username = summary.NewUsername;
						NHibernateRepository.Current.SaveOrUpdate<User>(user);

						//
						// Update the PageContent.EditedBy history
						//
						IList<PageContent> pageContents = PageContents.Where(p => p.EditedBy == summary.ExistingUsername).ToList();
						for (int i = 0; i < pageContents.Count; i++)
						{
							pageContents[i].EditedBy = summary.NewUsername;
							NHibernateRepository.Current.SaveOrUpdate<PageContent>(pageContents[i]);
						}

						//
						// Update all Page.CreatedBy and Page.ModifiedBy
						//
						IList<Page> pages = Pages.Where(p => p.CreatedBy == summary.ExistingUsername).ToList();
						for (int i = 0; i < pages.Count; i++)
						{
							pages[i].CreatedBy = summary.NewUsername;
							NHibernateRepository.Current.SaveOrUpdate<Page>(pages[i]);
						}

						pages = Pages.Where(p => p.ModifiedBy == summary.ExistingUsername).ToList();
						for (int i = 0; i < pages.Count; i++)
						{
							pages[i].ModifiedBy = summary.NewUsername;
							NHibernateRepository.Current.SaveOrUpdate<Page>(pages[i]);
						}
					}
					else
					{
						return false;
					}
				}

				return true;
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred updating the user {0} ", summary.ExistingEmail);
			}
		}

		/// <summary>
		/// Determines whether the user with the given email exists.
		/// </summary>
		/// <param name="email">The email address or username of the user.</param>
		/// <returns>
		/// true if the user exists;false otherwise.
		/// </returns>
		/// <exception cref="SecurityException">An NHibernate (database) error occurred while checking the email/user.</exception>
		public override bool UserExists(string email)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Email == email);
				return (user != null);
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred checking if user email {0} exists", email);
			}
		}


		/// <summary>
		/// Determines whether the user with the given username exists.
		/// </summary>
		/// <param name="username"></param>
		/// <returns>
		/// true if the user exists;false otherwise.
		/// </returns>
		public override bool UserNameExists(string username)
		{
			try
			{
				User user = Users.FirstOrDefault(u => u.Username == username);
				return (user != null);
			}
			catch (HibernateException ex)
			{
				throw new SecurityException(ex, "An error occurred checking if username {0} exists", username);
			}
		}
	}
}
