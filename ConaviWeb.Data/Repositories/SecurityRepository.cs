using Dapper;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ConaviWeb.Data.Repositories
{
    public class SecurityRepository:ISecurityRepository
    {
        private readonly MySQLConfiguration _connectionString;
        public SecurityRepository(MySQLConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected MySqlConnection DbConnection()
        {
            return new MySqlConnection(_connectionString.UserConnectionString);
        }
        public async Task<UserResponse> GetLoginByCredentials(UserRequest login)
        {
            try
            {

            
            var db = DbConnection();

            var sql = @"
                        SELECT u.id AS Id, nombre Name, usuario AS SUser, id_rol AS Rol, email Email
                        FROM sedatu.usuario u 
                        WHERE usuario = @SUser AND password = @Password AND estatus = 1";

            return await db.QueryFirstOrDefaultAsync<UserResponse>(sql, new { login.SUser, login.Password });
            }
            catch (System.Exception e)
            {

                throw;
            }
        }
        public async Task<UserResponse> GetLoginByUserId(int userId)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id AS Id, usuario AS SUser, id_rol AS Rol
                        FROM usuario WHERE id = @UserId AND activo = 1";

            return await db.QueryFirstOrDefaultAsync<UserResponse>(sql, new { UserId = userId });
        }
        public async Task<IEnumerable<Module>> GetModules(int idUser)
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Text, url Url, icono Ico, id_padre Orden
                        FROM  sedatu.c_modulo cm 
                        WHERE FIND_IN_SET(cm.id,(select id_modulo from sedatu.usuario u where id = @Id));";

            return await db.QueryAsync<Module>(sql, new {Id = idUser });
        }

        public async Task<bool> CreateUserSisevive(UserRequest userRequest)
        {
            var db = DbConnection();

            var sql = @"
                        CALL qa_adms_conavi.sp_create_user_sisevive(@Name, @LName, @SLName, @SUser, @Password, @Email);";

            var result = await db.ExecuteAsync(sql, new { 
                userRequest.SUser, userRequest.Name, userRequest.LName, userRequest.SLName, userRequest.Password, userRequest.Email });
            return result > 0;
        }

        public async Task<IEnumerable<Catalogo>> GetSistema(string nameSystem, int idSystem)
        {
            var db = DbConnection();
            var sql = "";
            if (idSystem != 0)
            {
                sql = @"
                        SELECT id Id, descripcion Descripcion FROM qa_adms_conavi.c_sistema where id = @IdSystem;";
            }
            else
            {
                sql = @"
                        SELECT id Id, descripcion Descripcion FROM qa_adms_conavi.c_sistema where descripcion = @NameSytem;";
            }
            return await db.QueryAsync<Catalogo>(sql, new { NameSytem = nameSystem , IdSystem = idSystem });
        }

        public async Task<bool> UpdatePassword(int idUser, string password)
        {
            var db = DbConnection();

            var sql = @"
                        UPDATE qa_adms_conavi.usuario
                            SET password = sha2(@password,256),
                                update_pass = 0
                        WHERE id = @idUser;";

            var result = await db.ExecuteAsync(sql, new
            {
                idUser,
                password,

            });
            return result > 0;
        }
        public async Task<IEnumerable<Catalogo>> GetSistemas()
        {
            var db = DbConnection();

            var sql = @"
                        SELECT id Id, descripcion Descripcion, ico Ico
                        FROM qa_adms_conavi.c_sistema";

            return await db.QueryAsync<Catalogo>(sql, new { });
        }
    }
}
