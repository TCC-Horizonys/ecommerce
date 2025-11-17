namespace ecommerce.Services
{
    public static class Funcoes
    {
        public static string FormatarCPF(this string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return string.Empty;

            cpf = new string(cpf.Where(char.IsDigit).ToArray());
            if (cpf.Length != 11) return cpf;

            return Convert.ToUInt64(cpf).ToString(@"000\.000\.000\-00");
        }
        public static string FormatarTelefone(this string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone)) return string.Empty;

            telefone = new string(telefone.Where(char.IsDigit).ToArray());

            if (telefone.Length == 11)
                return Convert.ToUInt64(telefone).ToString(@"(00)00000\-0000");
            else if (telefone.Length == 10)
                return Convert.ToUInt64(telefone).ToString(@"(00)0000\-0000");

            return telefone;

        }
    }
}
