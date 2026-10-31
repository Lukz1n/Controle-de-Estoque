# Controle de Estoque

Um aplicativo desktop para acompanhar produtos, registrar entradas e saídas e consultar o histórico do estoque. Os dados ficam salvos localmente no computador, em um banco SQLite.

## O que dá para fazer

- Consultar e pesquisar produtos pelo nome.

- Ver o total de produtos, unidades disponíveis e itens com estoque baixo.

- Cadastrar produtos, informar um código opcional e editar os dados depois.

- Registrar rapidamente a retirada de uma unidade ou informar outra quantidade.

- Adicionar unidades ao estoque e incluir uma observação na movimentação.

- Desativar produtos sem apagar o histórico relacionado a eles.

- Consultar movimentações e filtrar por produto, tipo e período.

- Exportar o estoque atual para Excel (`.xlsx`) ou PDF.

- Exportar o histórico filtrado para Excel ou PDF.

Na primeira execução, o sistema cadastra **Produto 1**, com 54 unidades, e **Produto 2**, com 23. Essa carga é feita uma única vez; as quantidades não são restauradas quando o programa é aberto novamente.

## Para executar no Windows

O projeto foi criado para Windows 10/11, com .NET 8 e Windows Forms. Para abrir pelo Visual Studio 2022, instale a carga de trabalho **Desenvolvimento para desktop com .NET**, abra `ControleEstoque.csproj` e pressione **F5**. O Visual Studio restaura os pacotes NuGet necessários na primeira compilação.

Também é possível executar pelo terminal, na pasta do projeto:

```
dotnet restore .\ControleEstoque.csproj
dotnet run --project .\ControleEstoque.csproj
```

Para gerar uma compilação de Release:

```
dotnet build .\ControleEstoque.csproj -c Release
```

É necessário ter o SDK do .NET 8 instalado. O acesso à internet é usado para restaurar os pacotes na primeira compilação; o aplicativo não depende de conexão para as operações normais de estoque.

## Onde os dados ficam

O banco é criado automaticamente em:

```
%LocalAppData%\ControleEstoque\estoque.db
```

O log de erros fica em `%LocalAppData%\ControleEstoque\logs\aplicativo.log`. Para fazer uma cópia de segurança, feche o programa e copie a pasta `%LocalAppData%\ControleEstoque` para um local seguro.

## Tecnologias

| Parte | Tecnologia |
| --- | --- |
| Interface | C# e Windows Forms |
| Plataforma | .NET 8 |
| Banco de dados | SQLite com `Microsoft.Data.Sqlite` |
| Planilhas | `ClosedXML` |
| Relatórios PDF | `QuestPDF` |

## Organização do projeto

```
ControleEstoque/
├── Data/       Banco de dados e inicialização do SQLite
├── Forms/      Janelas do aplicativo
├── Helpers/    Recursos compartilhados da interface e log
├── Models/     Modelos de produtos e movimentações
└── Services/   Regras de negócio, consultas e exportações
```

## Licenciamento

O repositório ainda não contém um arquivo `LICENSE`. Se você pretende permitir que outras pessoas reutilizem ou distribuam o código, escolha e inclua uma licença para o projeto. A biblioteca QuestPDF também tem seus próprios termos: o código está configurado com `LicenseType.Community`, então confira as condições de uso da biblioteca antes de distribuir o aplicativo, especialmente em contexto comercial.
