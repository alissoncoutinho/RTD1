CREATE OR ALTER VIEW [dbo].[AdministradorBarragemView]
AS
select	b.Id as idBarragem, 
		b.nome as nomeBarragem, 
		max(prof.UserId) as userId,  
		max(prof.nome) as userName, 
		max(prof.telefoneCelular) as telefone
from Barragem b
left join UserProfile prof
	on b.id= prof.barragemId
left join webpages_UsersInRoles usr_roles
	on prof.UserId=usr_roles.UserId
left join webpages_Roles roles
	on usr_roles.RoleId=roles.RoleId
where b.isativa=1 
 and roles.RoleId IN(2,3,4,5,6)
 and prof.situacao != 'desativado'
 group by b.Id, b.nome
