Colinha: API ASP.NET Core com Integração SAP (Nwrfc.net)
Guia rápido para criar uma API em ASP.NET Core (.NET 8+) que se comunica com uma RFC do SAP usando a biblioteca Nwrfc.net.

❗ Pré-requisitos Críticos
Antes de começar, garanta que o SAP NetWeaver RFC SDK está instalado e acessível.

Baixar o SAP NW RFC SDK do SAP Marketplace.

Copiar as DLLs da pasta lib (ex: sapnwrfc.dll, icudt72.dll, etc.) para a pasta de saída do seu projeto (ex: bin/Debug/net8.0) OU adicionar a pasta lib à variável de ambiente PATH do sistema.

Sem isso, a aplicação não vai rodar!

📂 Estrutura do Projeto
Uma estrutura organizada é a chave para a manutenção.

SuaApiSapIntegration/
|
├── Configuration/
|   └── SapConConfig.cs         # Classe para mapear as configurações de conexão
|
├── Controllers/
|   └── SapController.cs          # O "Atendente": recebe requisições web
|
├── Dtos/
|   ├── Entrada/
|   |   └── JsonEntradaPlanoDto.cs  # DTO para o JSON "plano" que a API recebe
|   └── Sap/
|       ├── CriarClienteRequest.cs  # DTO estruturado que o Serviço espera
|       └── SapLogRetornoDto.cs     # DTO para a resposta do SAP
|
├── Services/
|   ├── ISapService.cs          # A "Interface" (contrato) do serviço
|   └── SapService.cs           # A "Cozinha": contém a lógica de negócio e SAP
|
├── appsettings.json
└── Program.cs
Passo 1: Instalação via NuGet
Adicione o conector ao seu projeto.

Bash

dotnet add package Nwrfc.net
Passo 2: Configuração (appsettings.json)
Adicione as credenciais e dados de conexão do SAP. Use o User Secrets para dados sensíveis em desenvolvimento.

JSON

{
  "SapConnection": {
    "User": "SEU_USUARIO_SERVICO",
    "Passwd": "SUA_SENHA_AQUI",
    "Ashost": "ip.do.servidor.sap",
    "Sysnr": "00",
    "Client": "100",
    "Lang": "PT"
  }
}
Passo 3: Modelando os DTOs (Dtos/)
Defina os "contratos" de dados da sua aplicação.

JsonEntradaPlanoDto.cs (O que a API recebe)
C#

// /Dtos/Entrada/JsonEntradaPlanoDto.cs
namespace SuaApi.Dtos.Entrada;

// Representa o JSON "plano" que vem de um sistema legado/externo.
public class JsonEntradaPlanoDto
{
    public string NomeCliente { get; set; }
    public string CnpjCliente { get; set; }
    public string NomeRecebedor { get; set; }
}
CriarClienteRequest.cs (O que o Serviço usa)
C#

// /Dtos/Sap/CriarClienteRequest.cs
namespace SuaApi.Dtos.Sap;

// Representa a estrutura organizada que a lógica de negócio (Service) espera.
public class CriarClienteRequest
{
    public DadosClienteDto DadosCliente { get; set; }
    public DadosRecebedorDto DadosRecebedor { get; set; }
}

public class DadosClienteDto
{
    public string Nome { get; set; }
    public string Cnpj { get; set; }
}

public class DadosRecebedorDto
{
    public string Nome { get; set; }
}
SapLogRetornoDto.cs (A resposta do SAP)
C#

// /Dtos/Sap/SapLogRetornoDto.cs
namespace SuaApi.Dtos.Sap;

// Representa a estrutura de LOG/retorno que a RFC do SAP devolve.
public class SapLogRetornoDto
{
    public string Tipo { get; set; } // 'S' para Sucesso, 'E' para Erro, 'W' para Aviso
    public string Mensagem { get; set; }
}
Passo 4: Injeção de Dependência (Program.cs e SapConConfig.cs)
Conecte todas as peças da aplicação.

SapConConfig.cs
C#

// /Configuration/SapConConfig.cs
namespace SuaApi.Configuration;

// Classe que espelha a seção "SapConnection" do appsettings.json
public class SapConConfig
{
    public string User { get; set; }
    public string Passwd { get; set; }
    public string Ashost { get; set; }
    public string Sysnr { get; set; }
    public string Client { get; set; }
    public string Lang { get; set; }
}
Program.cs
C#

using SuaApi.Configuration;
using SuaApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Registra a classe de configuração para ser preenchida com os dados do appsettings.json
builder.Services.Configure<SapConConfig>(builder.Configuration.GetSection("SapConnection"));

// 2. Adiciona os serviços padrão da API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Registra nosso serviço de SAP (Injeção de Dependência)
// Quando um Controller pedir por "ISapService", o .NET entregará uma instância de "SapService"
builder.Services.AddScoped<ISapService, SapService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
Passo 5: Criando o Serviço de Lógica (Services/)
A "cozinha" onde a comunicação com o SAP acontece.

ISapService.cs (O Contrato)
C#

// /Services/ISapService.cs
using SuaApi.Dtos.Sap;

namespace SuaApi.Services;

public interface ISapService
{
    // Define que o serviço deve ter um método que recebe o DTO estruturado e devolve o log do SAP.
    Task<SapLogRetornoDto> ChamarRfcClienteAsync(CriarClienteRequest request);
}
SapService.cs (A Implementação)
C#

// /Services/SapService.cs
using Microsoft.Extensions.Options;
using Nwrfc.net;
using SuaApi.Configuration;
using SuaApi.Dtos.Sap;

namespace SuaApi.Services;

public class SapService : ISapService
{
    private readonly SapConConfig _sapConfig;

    // O .NET injeta a configuração que registramos no Program.cs
    public SapService(IOptions<SapConConfig> sapConfigOptions)
    {
        _sapConfig = sapConfigOptions.Value;
    }

    public async Task<SapLogRetornoDto> ChamarRfcClienteAsync(CriarClienteRequest request)
    {
        // Converte a classe de config para o dicionário que o conector Nwrfc.net espera
        var connectionParams = new ConnectionParameters
        {
            { ConnectionParameters.User, _sapConfig.User },
            { ConnectionParameters.Password, _sapConfig.Passwd },
            { ConnectionParameters.AppServerHost, _sapConfig.Ashost },
            { ConnectionParameters.SystemNumber, _sapConfig.Sysnr },
            { ConnectionParameters.Client, _sapConfig.Client },
            { ConnectionParameters.Language, _sapConfig.Lang },
        };

        try
        {
            // Usar Task.Run para não bloquear a thread principal da API em chamadas longas.
            return await Task.Run(() =>
            {
                // 'using' garante que a conexão com o SAP seja sempre fechada, mesmo se ocorrer um erro.
                using (var connection = new Connection(connectionParams))
                {
                    connection.Open();
                    var function = connection.CreateFunction("NOME_DA_SUA_RFC"); // <-- Mude para o nome real

                    // Mapeia os dados do nosso DTO para a estrutura de IMPORT da RFC
                    var dadosClienteIn = function.GetStructure("DADOS_CLIENTE");
                    dadosClienteIn.SetValue("NOME_CLIENTE_SAP", request.DadosCliente.Nome);
                    dadosClienteIn.SetValue("CNPJ_CLIENTE_SAP", request.DadosCliente.Cnpj);

                    var dadosRecebedorIn = function.GetStructure("DADOS_RECEBEDOR");
                    dadosRecebedorIn.SetValue("NOME_RECEBEDOR_SAP", request.DadosRecebedor.Nome);

                    // Executa a chamada para o SAP
                    function.Invoke();

                    // Lê a estrutura de EXPORT (ou CHANGING) da RFC
                    var logOut = function.GetStructure("LOG");
                    return new SapLogRetornoDto
                    {
                        Tipo = logOut.GetValue<string>("TIPO"),
                        Mensagem = logOut.GetValue<string>("MENSAGEM")
                    };
                }
            });
        }
        catch (NwrfcConnectionException ex)
        {
            throw new Exception($"Erro de CONEXÃO com o SAP: {ex.Message}", ex);
        }
        catch (NwrfcInvokeException ex)
        {
            throw new Exception($"Erro na EXECUÇÃO da RFC no SAP: {ex.Message}", ex);
        }
    }
}
Passo 6: Expondo o Endpoint (Controllers/)
O "atendente" que recebe a chamada HTTP e orquestra o fluxo.

SapController.cs
C#

// /Controllers/SapController.cs
using Microsoft.AspNetCore.Mvc;
using SuaApi.Dtos.Entrada;
using SuaApi.Dtos.Sap;
using SuaApi.Services;

namespace SuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SapController : ControllerBase
{
    private readonly ISapService _sapService;

    // O .NET injeta o serviço que registramos no Program.cs
    public SapController(ISapService sapService)
    {
        _sapService = sapService;
    }

    [HttpPost("criar-cliente")]
    public async Task<IActionResult> CriarCliente([FromBody] JsonEntradaPlanoDto requestPlano)
    {
        try
        {
            // --- ETAPA DE TRANSFORMAÇÃO (Adapter) ---
            // Converte o DTO "plano" da entrada para o DTO "estruturado" que o serviço espera.
            var requestEstruturado = new CriarClienteRequest
            {
                DadosCliente = new DadosClienteDto
                {
                    Nome = requestPlano.NomeCliente,
                    Cnpj = requestPlano.CnpjCliente
                },
                DadosRecebedor = new DadosRecebedorDto
                {
                    Nome = requestPlano.NomeRecebedor
                }
            };

            // Chama o serviço com o objeto JÁ ESTRUTURADO.
            var resultadoSap = await _sapService.ChamarRfcClienteAsync(requestEstruturado);

            // Se o SAP retornou um erro de negócio, retorna um status HTTP 400 (Bad Request).
            if ("EA".Contains(resultadoSap.Tipo)) // E = Error, A = Abort
            {
                return BadRequest(resultadoSap);
            }

            // Retorna sucesso (200 OK) com os dados do log.
            return Ok(resultadoSap);
        }
        catch (Exception ex)
        {
            // Se qualquer outra exceção ocorrer, retorna um erro 500 (Internal Server Error).
            // Em produção, logar o `ex` em um sistema de logging.
            return StatusCode(500, new { Erro = "Ocorreu uma falha interna no servidor.", Detalhes = ex.Message });
        }
    }
}
