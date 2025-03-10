﻿using ConaviWeb.Model;
using ConaviWeb.Model.Minuta;
using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConaviWeb.Data.Minuta
{
    public class MinutaRepository : IMinutaRepository
    {
        private readonly MySQLConfiguration _connectionString;
        public MinutaRepository(MySQLConfiguration connectionString)
        {
            _connectionString = connectionString;
        }
        protected MySqlConnection DbConnection()
        {
            return new MySqlConnection(_connectionString.ConnectionString);
        }
        public async Task<IEnumerable<Catalogo>> GetSector()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                descripcion AS Descripcion 
                            FROM sedatu.c_sector;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetEntidad()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                clave Clave, 
                                descripcion AS Descripcion 
                            FROM sedatu.c_entidad WHERE clave not in (99);
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetMunicipio(string clave)
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                cv_mun Clave, 
                                descripcion AS Descripcion 
                            FROM sedatu.c_municipio WHERE cv_ef = @Clave;
                         ";

            return await db.QueryAsync<Catalogo>(sql ,new { Clave = clave});
        }
        public async Task<IEnumerable<Catalogo>> GetResponsable()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                nombre AS Descripcion 
                            FROM sedatu.c_personal;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetGestion()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                descripcion AS Descripcion 
                            FROM sedatu.c_gestion;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetMesesAcu()
        {
            var db = DbConnection();

            var sql = @"
                            select EXTRACT( YEAR_MONTH FROM `created_at` ) clave  from sedatu.acuerdo a group by clave;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetMesesReu()
        {
            var db = DbConnection();

            var sql = @"
                            select EXTRACT( YEAR_MONTH FROM fch_carga ) clave  from sedatu.reunion r group by clave;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetEstatus()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                descripcion AS Descripcion 
                            FROM sedatu.c_estatus;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetReunion(int id)
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                asunto AS Descripcion 
                            FROM sedatu.reunion
                            WHERE id = @Id;
                         ";

            return await db.QueryAsync<Catalogo>(sql, new { Id = id});
        }
        public async Task<IEnumerable<Catalogo>> GetReunion()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                CONCAT_WS(' - ',id,asunto) AS Descripcion 
                            FROM sedatu.reunion where estatus in (1,2)  and id not in (select id_reunion from sedatu.minuta);
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetReunionMinuta()
        {
            var db = DbConnection();

            var sql = @"
                          SELECT 
                                id Clave, 
                                CONCAT_WS(' - ',concat('R',id),asunto) AS Descripcion 
                            FROM sedatu.reunion
                            where estatus in (1, 2)  and id not in (select id_reunion from sedatu.minuta)
                            union all
                            SELECT
                                id Clave, 
                                CONCAT_WS(' - ', concat('RT', id), asunto) AS Descripcion
                            FROM sedatu.reunion_titular rt;";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<IEnumerable<Catalogo>> GetTemas()
        {
            var db = DbConnection();

            var sql = @"
                            SELECT 
                                id Clave, 
                                descripcion 
                            FROM sedatu.c_tema;
                         ";

            return await db.QueryAsync<Catalogo>(sql);
        }
        public async Task<ReunionResponse> GetReunionDetail(int id)
        {
            var reunion = new ReunionResponse();
            var db = DbConnection();
            var results = await db.QueryMultipleAsync(
                @"
                            select r.id id,cs.descripcion sector,ce.descripcion EntidadFed,cm.descripcion Municipio,DATE_FORMAT(fch_solicitud,'%d/%m/%Y') FechaSolicitud,DATE_FORMAT(fch_atencion,'%d/%m/%Y') FechaAtencion,asunto,nombre_sol nombreSol,cargo_sol cargoSol,dependencia_sol dependenciaSol,r.nombre_cont nombreCont,r.telefono_cont telefonoCont,r.email_cont emailCont,antecedentes,observaciones,cp.nombre responsable ,cg.descripcion gestion,e.descripcion estatus
                            from sedatu.reunion r
                            join sedatu.c_sector cs on cs.id = r.id_sector
                            left join sedatu.c_entidad ce on ce.clave = r.cv_ef
                            left join sedatu.c_municipio cm on cm.cv_ef = r.cv_ef and cm.cv_mun = r.cv_mun
                            left join sedatu.c_personal cp on cp.id = r.id_responsable
                            join sedatu.c_gestion cg on cg.id = r.id_gestion
                            join sedatu.c_estatus e on e.id = r.estatus
                            WHERE r.id = @Id;

                            select a.id,a.id_reunion IdReunion,a.interno, a.externo, DATE_FORMAT(a.fecha_termino,'%d/%m/%Y') FechaTermino,a.descripcion,cg.descripcion gestion,e.descripcion estatus 
                            from sedatu.acuerdo a
                            join sedatu.reunion r on r.id = a.id_reunion 
                            join sedatu.c_gestion cg on cg.id = a.id_gestion
                            join sedatu.c_estatus e on e.id = a.estatus
                            WHERE a.id_reunion = @Id AND cv_bajal = 1;
                         ", new { Id = id});
            reunion = await results.ReadFirstOrDefaultAsync<ReunionResponse>();
            reunion.Acuerdos = (List<AcuerdoResponse>)await results.ReadAsync<AcuerdoResponse>();
                        
            return reunion;
        }
        public async Task<ReunionResponse> GetRMinuta(int id)
        {
            var db = DbConnection();
            var sql = @"
                    select r.id id,cs.descripcion sector,ce.descripcion EntidadFed,cm.descripcion Municipio,DATE_FORMAT(fch_solicitud,'%d/%m/%Y') FechaSolicitud,DATE_FORMAT(fch_atencion,'%d/%m/%Y') FechaAtencion,asunto,nombre_sol nombreSol,cargo_sol cargoSol,dependencia_sol dependenciaSol,r.nombre_cont nombreCont,r.telefono_cont telefonoCont,r.email_cont emailCont,antecedentes,observaciones,cp.nombre responsable ,cg.descripcion gestion,e.descripcion estatus
                            from sedatu.reunion r
                            join sedatu.c_sector cs on cs.id = r.id_sector
                            join sedatu.c_entidad ce on ce.clave = r.cv_ef
                            join sedatu.c_municipio cm on cm.cv_ef = r.cv_ef and cm.cv_mun = r.cv_mun
                            left join sedatu.c_personal cp on cp.id = r.id_responsable
                            join sedatu.c_gestion cg on cg.id = r.id_gestion
                            join sedatu.c_estatus e on e.id = r.estatus
                            WHERE r.id = @Id;";
                return await db.QueryFirstOrDefaultAsync<ReunionResponse>(sql, new {Id = id });
            
        }
        public async Task<ReunionTitular> GetRTMinuta(int id)
        {
            //var reunion = new ReunionResponse();
            var db = DbConnection();
            var sql = @"
                select rt.id,ct.descripcion tema, responsable, modalidad, liga, asunto, cs.descripcion sector, nombreSol, cargoSol, instSol, objetivo, objetivo antecedentes, lugar, sala, tiempo, celular radioCel,p_sedatu pSedatu, p_externo pExterno,accesos radioAD, l_accesos accesoD,orden,insumos,estatus,fch_carga FchCarga
                            from sedatu.reunion_titular rt
                            join sedatu.c_tema ct on ct.id = rt.id_tema
                            join sedatu.c_sector cs on cs.id = rt.id_sector
                            where rt.id = @Id;";
                
                return await db.QueryFirstOrDefaultAsync<ReunionTitular>(sql , new { Id = id });

        }
        public async Task<AcuerdoResponse> GetAcuerdoDetail(int id)
        {
            var reunion = new AcuerdoResponse();
            var db = DbConnection();
            var results = await db.QueryMultipleAsync(
                @"
                            select a.id,a.id_reunion IdReunion, a.interno, a.externo, DATE_FORMAT(a.fecha_termino,'%d/%m/%Y') FechaTermino,a.descripcion,cg.descripcion gestion,e.descripcion estatus, a.estatus idEstatus from sedatu.acuerdo a 
                            join sedatu.reunion r on r.id = a.id_reunion 
                            join sedatu.c_gestion cg on cg.id = a.id_gestion
                            join sedatu.c_estatus e on e.id = a.estatus
                            WHERE a.id = @Id AND cv_bajal = 1;
                            select id,id_acuerdo idAcuerdo,archivo NombreArchivo,created_by Usuario,created_at Fecha,estatus
                            from sedatu.archivo_acuerdo
                            WHERE id_acuerdo = @Id AND estatus = 1;
                         ", new { Id = id });
            reunion = await results.ReadFirstOrDefaultAsync<AcuerdoResponse>();
            reunion.Archivo = (List<ArchivoAcuerdo>)await results.ReadAsync<ArchivoAcuerdo>();
            return reunion;
        }
        public async Task<Model.Minuta.Minuta> GetMinuta(int id)
        {
            var db = DbConnection();

            var sql = @"
                            select m.id, m.id_reunion idReunion, ct.descripcion tema, r.asunto, m.interno, m.externo, r.antecedentes contexto, m.descripcion, m.estatus, m.created_at FchCreate
                            from sedatu.minuta m
                            join sedatu.c_tema ct on ct.id = m.id_tema
                            join sedatu.reunion r on r.id = m.id_reunion
                            where m.id_reunion = @Id;
                         ";

            return await db.QueryFirstOrDefaultAsync<Model.Minuta.Minuta>(sql, new { Id = id });
        }
        public async Task<Model.Minuta.Minuta> GetMinutaRT(int id)
        {
            var db = DbConnection();

            var sql = @"
                            select m.id, m.id_rtitular idRTitular, ct.descripcion tema, r.asunto, m.interno, m.externo, r.objetivo contexto, m.descripcion, m.estatus, m.created_at FchCreate
                            from sedatu.minuta m
                            join sedatu.c_tema ct on ct.id = m.id_tema
                            join sedatu.reunion_titular r on r.id = m.id_rtitular 
                            where m.id_rtitular = @Id;
                         ";

            return await db.QueryFirstOrDefaultAsync<Model.Minuta.Minuta>(sql, new { Id = id });
        }
        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdos(int id)
        {
            var lookup = new Dictionary<int, Model.Minuta.AcuerdoResponse>();
            var db = DbConnection();
            var sql = @"
                            select a.id,id_reunion idReunion, interno, externo, DATE_FORMAT(a.fecha_termino,'%d/%m/%Y') fechaTermino ,a.descripcion,cg.descripcion gestion ,'' area,e.descripcion estatus
                            from sedatu.acuerdo a
                            join sedatu.c_gestion cg on cg.id = a.id_gestion 
                            join sedatu.c_estatus e on e.id = a.estatus
                            where id_reunion  = @Id;
                           ";
            return await db.QueryAsync<AcuerdoResponse>(sql, new { Id = id });
        }
        public async Task<ReunionTitular> GetRTitular(int id)
        {
            var db = DbConnection();
            var sql = @"
                            select rt.id,ct.descripcion tema,responsable,modalidad,liga,asunto,cs.descripcion sector,nombreSol,cargoSol,instSol,objetivo,lugar,sala,tiempo,celular radioCel,p_sedatu pSedatu,p_externo pExterno,accesos radioAD,l_accesos accesoD,orden,insumos,estatus,fch_carga FchCarga
                            from sedatu.reunion_titular rt
                            join sedatu.c_tema ct on ct.id = rt.id_tema 
                            join sedatu.c_sector cs on cs.id = rt.id_sector
                            where rt.id  = @Id;
                           ";
            return await db.QueryFirstOrDefaultAsync<ReunionTitular>(sql, new { Id = id });
        }

        public async Task<IEnumerable<ReunionTitular>> GetRTitulares()
        {
            var db = DbConnection();
            var sql = @"
                            select rt.id,ct.descripcion tema,responsable,modalidad,liga,asunto,cs.descripcion sector,nombreSol,cargoSol,instSol,objetivo,lugar,sala,tiempo,celular radioCel,p_sedatu pSedatu,p_externo pExterno,accesos radioAD,l_accesos accesoD,orden,insumos,estatus,fch_carga FchCarga
                            from sedatu.reunion_titular rt
                            join sedatu.c_tema ct on ct.id = rt.id_tema 
                            join sedatu.c_sector cs on cs.id = rt.id_sector;
                           ";
            return await db.QueryAsync<ReunionTitular>(sql, new { });
        }
        public async Task<bool> InsertArchivoAcuerdo(ArchivoAcuerdo archivo)
        {
            var db = DbConnection();

            var sql = @"
                        INSERT INTO sedatu.archivo_acuerdo (id_acuerdo, archivo, created_by)
                        VALUES (@IdAcuerdo,@NombreArchivo,@Usuario);
                    ";

            var result = await db.ExecuteAsync(sql, new
            {
                archivo.IdAcuerdo,
                archivo.NombreArchivo,
                archivo.Usuario
            });
            return result > 0;

        }
        public async Task<bool> InsertReunion(Reunion reunion)
        {
            var db = DbConnection();

            var sql = @"
                        CALL sedatu.sp_inserta_reunion(@Sector,@EntidadFed,@Modalidad,@Municipio,@Asunto,@FechaSolicitud,@NombreSol,@CargoSol,@DependenciaSol,@NombreCont,@TelefonoCont,@EmailCont,@Antecedentes,@Responsable,@FechaAtencion,@Observaciones,@Gestion,@IdEstatus,@CreatedBy);";

            var result = await db.ExecuteAsync(sql, new {
                reunion.Sector,
                reunion.EntidadFed,
                reunion.Modalidad,
                reunion.Municipio,
                reunion.Asunto,
                reunion.FechaSolicitud,
                reunion.NombreSol,
                reunion.CargoSol,
                reunion.DependenciaSol,
                reunion.NombreCont,
                reunion.TelefonoCont,
                reunion.EmailCont,
                reunion.Antecedentes,
                reunion.Responsable,
                reunion.FechaAtencion,
                reunion.Observaciones,
                reunion.Gestion,
                reunion.IdEstatus,
                reunion.CreatedBy
            });
            return result > 0;

        }
        public async Task<bool> InsertReunion(ReunionTitular reunion)
        {
            var db = DbConnection();

            var sql = @"
                        INSERT INTO sedatu.reunion_titular
                        (id_tema, responsable, modalidad, liga, asunto, id_sector, nombreSol, cargoSol, instSol, objetivo, lugar, sala, tiempo, celular, p_sedatu, p_externo, accesos, l_accesos, orden, insumos, created_by)
                        VALUES
                        (@Tema,@Responsable,@Modalidad,@Liga,@Asunto,@Sector,@NombreSol,@CargoSol,@InstSol,@Objetivo,@Lugar,@Sala,@Tiempo,@RadioCel,@PSedatu,@PExterno,@radioAD,@AccesoD,@Orden,@Insumos,@CreatedBy);";

            var result = await db.ExecuteAsync(sql, new
            {
                reunion.Tema,
                reunion.Responsable,
                reunion.Modalidad,
                reunion.Liga,
                reunion.Asunto,
                reunion.Sector,
                reunion.NombreSol,
                reunion.CargoSol,
                reunion.InstSol,
                reunion.Objetivo,
                reunion.Lugar,
                reunion.Sala,
                reunion.Tiempo,
                reunion.RadioCel,
                reunion.PSedatu,
                reunion.PExterno,
                reunion.RadioAD,
                reunion.AccesoD,
                reunion.Orden,
                reunion.Insumos,
                reunion.CreatedBy

            });
            return result > 0;

        }
        public async Task<bool> InsertMinuta(Model.Minuta.Minuta minuta)
        {
            var db = DbConnection();
            var sql = @"
                        INSERT INTO sedatu.minuta (id_reunion, id_rtitular, id_tema, asunto, interno, externo, contexto, descripcion, created_by) VALUES(@IdReunion, @IdRTitular, @IdTema, @Asunto, @Interno, @Externo, @Contexto, @Descripcion,@CreatedBy ) ON DUPLICATE KEY UPDATE id_reunion = @IdReunion,id_tema = @IdTema, asunto = @Asunto, interno = @Interno, externo = @Externo, contexto = @Contexto, descripcion = @Descripcion, created_by = @CreatedBy;";
            var result = await db.ExecuteAsync(sql, minuta);
            return result > 0;
        }
        public async Task<bool> InsertParticipantes(IEnumerable<Participante> participantes)
        {
            var db = DbConnection();
            var sql = @"
                        INSERT INTO sedatu.participante (id_minuta, area, nombre, email) VALUES(@IdMinuta, @Direccion, @Nombre, @Email);";
            var result = await db.ExecuteAsync(sql, participantes);
            return result > 0;
        }
        public async Task<bool> InsertAcuerdo(Acuerdo acuerdo)
        {
            var db = DbConnection();
            var sql = @"
                        INSERT INTO sedatu.acuerdo (id_reunion, interno, externo, fecha_termino, descripcion, id_gestion, estatus, created_by) VALUES(@IdReunion, @Interno, @Externo, @FechaTermino, @Descripcion, @IdGestion, @IdEstatus,@CreatedBy);";
            var result = await db.ExecuteAsync(sql, acuerdo);
            return result > 0;
        }
        public async Task<IEnumerable<ReunionResponse>> GetReuniones()
        {
            var lookup = new Dictionary<int, Model.Minuta.ReunionResponse>();
            var db = DbConnection();
            var sql = @"
                            select r.id id, cs.descripcion sector, dependencia_sol dependenciaSol, ce.descripcion entidadFed, cm.descripcion municipio, r.asunto, r.nombre_sol nombreSol, DATE_FORMAT(fch_solicitud,'%d/%m/%Y') fechaSolicitud, e.descripcion estatus
                            from sedatu.reunion r 
                            join sedatu.c_sector cs on cs.id = r.id_sector 
                            left join sedatu.c_entidad ce on ce.clave = r.cv_ef 
                            left join sedatu.c_municipio cm on cm.cv_ef = r.cv_ef and cm.cv_mun = r.cv_mun
                            join sedatu.c_estatus e on e.id = r.estatus;
                           ";
            return await db.QueryAsync<ReunionResponse>(sql);
        }
        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdos()
        {
            var lookup = new Dictionary<int, Model.Minuta.AcuerdoResponse>();
            var db = DbConnection();
            var sql = @"
                            select a.id,id_reunion idReunion, interno, externo, DATE_FORMAT(a.fecha_termino,'%d/%m/%Y') fechaTermino ,a.descripcion,cg.descripcion gestion ,'' area,e.descripcion estatus
                            from sedatu.acuerdo a
                            join sedatu.c_gestion cg on cg.id = a.id_gestion 
                            join sedatu.c_estatus e on e.id = a.estatus;
                           ";
            return await db.QueryAsync<AcuerdoResponse>(sql);
        }
        public async Task<IEnumerable<ReunionIndicadores>> GetIndReunion(int id)
        {
            var sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus 
                            group by r.estatus;
                           ";
            var db = DbConnection();
            if(id != 0) { 
                sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus 
                            where r.id_gestion = @Id
                            group by r.estatus;
                           ";
            }
            return await db.QueryAsync<ReunionIndicadores>(sql,new { Id = id});
        }
        public async Task<IEnumerable<ReunionIndicadores>> GetIndReunionMes(int id, string clave)
        {
            var sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus,MONTH(fch_carga) numes from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus 
                            group by r.estatus, numes;";
            var db = DbConnection();
            if (id != 0 && clave != null)
            {
                sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus,MONTH(fch_carga) numes from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus 
                            where r.id_gestion = @Id and  EXTRACT( YEAR_MONTH FROM fch_carga ) = @Clave group by r.estatus, numes;";
            }
            else if (id != 0 && clave == null)
            {
                sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus,MONTH(fch_carga) numes from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus 
                            where r.id_gestion = @Id group by r.estatus, numes;";
            }
            else if (id == 0 && clave != null)
            {
                sql = @"
                            select COUNT(r.id) reuniones, e.descripcion estatus,MONTH(fch_carga) numes from sedatu.reunion r
                            join sedatu.c_estatus e on e.id = r.estatus
                            where  EXTRACT( YEAR_MONTH FROM fch_carga ) = @Clave group by r.estatus, numes;";
            }
            return await db.QueryAsync<ReunionIndicadores>(sql, new { Id = id, Clave = clave });
        }
        public async Task<IEnumerable<AcuerdoIndicadores>> GetIndAcuerdo(int id, string clave)
        {
            //var mes = clave != null ? "and EXTRACT( YEAR_MONTH FROM `created_at` ) = " + clave : "";
            var sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus group by a.estatus;";
            var db = DbConnection();
            if (id != 0)
            {
                sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus
                            where a.id_gestion = @Id group by a.estatus;";
            }
            
            return await db.QueryAsync<AcuerdoIndicadores>(sql, new { Id = id, Clave = clave });
        }
        public async Task<IEnumerable<AcuerdoIndicadores>> GetIndAcuerdoMes(int id, string clave)
        {
            //var mes = clave != null ? "and EXTRACT( YEAR_MONTH FROM `created_at` ) = " + clave : "";
            var sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus, MONTH(created_at) numes from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus group by a.estatus, numes;";
            var db = DbConnection();
            if (id != 0 && clave != null)
            {
                sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus, MONTH(created_at) numes from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus
                            where a.id_gestion = @Id and  EXTRACT( YEAR_MONTH FROM `created_at` ) = @Clave group by a.estatus, numes;";
            }
            else if (id != 0 && clave == null)
            {
                sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus, MONTH(created_at) numes from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus
                            where a.id_gestion = @Id group by a.estatus, numes;";
            }
            else if (id == 0 && clave != null)
            {
                sql = @"
                            select COUNT(a.id) acuerdos, e.descripcion estatus, MONTH(created_at) numes from sedatu.acuerdo a
                            join sedatu.c_estatus e on e.id = a.estatus
                            where  EXTRACT( YEAR_MONTH FROM `created_at` ) = @Clave group by a.estatus, numes;";
            }
            return await db.QueryAsync<AcuerdoIndicadores>(sql, new { Id = id, Clave = clave });
        }
        public async Task<bool> DeleteAcuerdo(int id)
        {
            var db = DbConnection();

            var sql = @"
                        update sedatu.acuerdo set cv_bajal = 2 where id = @Id ;
                       ";

            var result = await db.ExecuteAsync(sql,new { Id = id});
            return result > 0;

        }
        public async Task<bool> UpdateEstatusAcuerdo(int acuerdo, int estatus)
        {
            var db = DbConnection();

            var sql = @"
                        update sedatu.acuerdo set estatus = @Estatus where id = @Id;
                       ";

            var result = await db.ExecuteAsync(sql, new { Id = acuerdo, Estatus = estatus });
            return result > 0;

        }
        //public async Task<IEnumerable<Model.Minuta.ReunionResponse>> GetReuniones()
        //{
        //    var lookup = new Dictionary<int, Model.Minuta.ReunionResponse>();
        //    var db = DbConnection();
        //    var sql = @"
        //                    select r.id id, cs.descripcion sector, ce.descripcion entidadFed, cm.descripcion municipio, r.asunto, r.solicitante, p.id_minuta, p.area, p.nombre, p.email, a.id_minuta, a.responsable, a.dacuerdo, a.fecha_termino
        //                    from sedatu.reunion r 
        //                    join sedatu.c_sector cs on cs.id = r.id_sector 
        //                    join sedatu.c_entidad ce on ce.clave = r.cv_ef 
        //                    join sedatu.c_municipio cm on cm.cv_ef = r.cv_ef and cm.cv_mun = r.cv_mun 
        //                    left join sedatu.participante p on p.id_minuta = r.id
        //                    left join sedatu.acuerdo a on a.responsable = p.email and a.id_minuta = r.id;
        //                   ";
        //    _ = await db.QueryAsync<Model.Minuta.ReunionResponse, Participante, Acuerdo, Model.Minuta.ReunionResponse>(sql, (r, p, a) =>
        //    {
        //        if (!lookup.TryGetValue(r.Id, out var mEntry))
        //        {
        //            mEntry = r;
        //            mEntry.Participantes ??= new List<Participante>();
        //            mEntry.Acuerdos ??= new List<Acuerdo>();
        //            lookup.Add(r.Id, mEntry);
        //        }

        //        mEntry.Participantes.Add(p);
        //        mEntry.Acuerdos.Add(a);
        //        return mEntry;

        //    }, splitOn: "id_minuta");

        //    return lookup.Values.AsList();
        //}

    }
}
