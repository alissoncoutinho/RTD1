using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace Barragem.Models
{
    

    [Table("UserProfile")]
    public class UserProfile
    {
        public UserProfile(){
            this.altura = 1;
        }
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Display(Name = "login")]
        public string UserName { get; set; }

        [Display(Name = "nome (ou apelido)")]
        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string nome { get; set; }

        [Display(Name = "data nascimento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Campo data é obrigatório")]
        public DateTime dataNascimento { get; set; }

        [Display(Name = "naturalidade")]
        public string naturalidade { get; set; }

        public int? altura { get; set; }

        [Display(Name = "altura")]
        [Required(ErrorMessage = "Campo altura é obrigatório")]
        public string altura2 { get; set; }

        [Display(Name = "lateralidade")]
        public string lateralidade { get; set; }

        [Display(Name = "telefone")]
        public string telefoneFixo { get; set; }

        [Required(ErrorMessage = "O campo celular é obrigatório")]
        [Display(Name = "celular/operadora")]
        public string telefoneCelular { get; set; }

        public virtual string linkwhatsapp
        {
            get{
                var i = telefoneCelular.IndexOf("/");
                var dddcel = "";
                if (i < 1){
                    dddcel = telefoneCelular.Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
                } else {
                    dddcel = telefoneCelular.Substring(0, i).Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""); 
                }
                
                return "https://api.whatsapp.com/send?phone=55" + dddcel + "&text=Olá,%20" + nome + "!%20Temos%20um%20jogo%20de%20tênis%20nesta%20rodada,%20qual%20é%20a%20sua%20disponibilidade?";
            }
        }

        public virtual int idade
        {
            get {
                var birthdate = dataNascimento;
                var today =  DateTime.Now;
                var idade = today.Year - birthdate.Year;
                if (birthdate > today.AddYears(-idade))  idade--;
                return idade;
            }
        }

        [Display(Name = "celular2/operadora")]
        public string telefoneCelular2 { get; set; }

        [Required(ErrorMessage = "O campo bairro é obrigatório")]
        public string bairro { get; set; }

        [Display(Name = "Matrícula (apenas para clubes)")]
        public string matriculaClube { get; set; }

        [Display(Name = "Situação")]
        public string situacao { get; set; }

        [Required(ErrorMessage = "O campo email é obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }
        [Display(Name = "Foto")]
        public byte[] foto { get; set; }

        public string fotoURL { get; set; }

        [Display(Name = "Já possui rancking")]
        public bool isRanckingGerado { get; set; }

        public DateTime dataInicioRancking { get; set; }
        
        [Display(Name = "Nível de jogo")]
        public string nivelDeJogo { get; set; }

        [Display(Name = "Ranking")]
        public int barragemId { get; set; }

        [Display(Name = "Ranking")]
        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        [Display(Name = "Classe")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }

        public DateTime? dataAlteracao { get; set; }
        public string logAlteracao { get; set; }
    }

    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        public string ExternalLoginData { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação de senha não estão iguais.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Manter-me logado")]
        public bool RememberMe { get; set; }
    }


    public class LoginRankingModel
    {
        public string userName { get; set; }

        public string nomeRanking { get; set; }

        public int idRanking { get; set; }

        public int userId { get; set; }
    }

    public class RegisterModel
    {
        public RegisterModel(){
            this.altura = 1;
        }
        [Required(ErrorMessage = "O campo login é obrigatório")]
        [Display(Name = "Login")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "O campo senha é obrigatório")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        [Required(ErrorMessage = "O campo senha é obrigatório")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare("Password", ErrorMessage = "A senha e a confirmação de senha não estão iguais.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "nome (ou apelido)")]
        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string nome { get; set; }

        [Display(Name = "data nascimento")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date, ErrorMessage = "Data de Nascimento com formato inválido")]
        [Required(ErrorMessage = "Campo data nascimento obrigatório")]
        public DateTime dataNascimento { get; set; }
        [Display(Name = "naturalidade")]
        public string naturalidade { get; set; }

        public int? altura { get; set; }

        [Display(Name = "altura")]
        [Required(ErrorMessage = "Campo altura é obrigatório")]
        public string altura2 { get; set; }

        [Display(Name = "lateralidade")]
        public string lateralidade { get; set; }

        [Display(Name = "telefone")]
        public string telefoneFixo { get; set; }

        [Required(ErrorMessage = "O campo celular é obrigatório")]
        [Display(Name = "celular/operadora")]
        public string telefoneCelular { get; set; }

        [Display(Name = "celular2/operadora")]
        public string telefoneCelular2 { get; set; }

        [Required(ErrorMessage = "O campo bairro é obrigatório")]
        public string bairro { get; set; }

        [Display(Name = "Matrícula (apenas para clubes)")]
        public string matriculaClube { get; set; }
        public string situacao { get; set; }

        [Required(ErrorMessage = "O campo email é obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }
        [Display(Name = "Foto")]
        public byte[] foto { get; set; }

        public string fotoURL { get; set; }

        public DateTime dataInicioRancking { get; set; }
        
        [Display(Name = "Nível de jogo")]
        public string nivelDeJogo { get; set; }

        [Display(Name = "Barragem")]
        public int barragemId { get; set; }

        [Display(Name = "Barragem")]
        [ForeignKey("barragemId")]
        public virtual Barragens barragem { get; set; }

        [Display(Name = "Classe")]
        [Required(ErrorMessage = "Este campo é obrigatório")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }

        public bool organizador { get; set; }
        
    }

    public class ExternalLogin
    {
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
        public string ProviderUserId { get; set; }
    }

    public class ConfirmaSenhaModel
    {
        public string TokenId { get; set; }
        [Required(ErrorMessage = "Campo obrigatório")]
        public string Senha { get; set; }
        [Required(ErrorMessage = "Campo obrigatório")]
        public string SenhaConfirmacao { get; set; }
    }
    public class VerificacaoCadastro
    {
        public VerificacaoCadastro()
        {
            this.torneioId = 0;
        }
        [Required(ErrorMessage = "O campo email é obrigatório")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }

        public string returnUrl { get; set; }

        public int torneioId { get; set; }
    }

    public class RegisterInscricao{

        public RegisterInscricao() {
            this.register = new RegisterModel();
            this.register.bairro = "Não informado";
            this.register.classeId = 0;
            this.inscricao = new InscricaoTorneio();
        }

        public RegisterModel register { get; set; }

        public InscricaoTorneio inscricao { get; set; }
        public bool isMaisDeUmaClasse { get; set; }

        public int classeInscricao2 { get; set; }

        
    }
}
