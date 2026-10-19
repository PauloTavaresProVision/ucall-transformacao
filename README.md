# UCALL Transformação

## Colocar a aplicação no servidor

A instalação cria automaticamente aplicação, PostgreSQL, base de dados, credenciais e volumes. Não pede domínio, rede ou dados de PostgreSQL. Requer Docker e Docker Compose v2 já instalados.

Primeira instalação no PuTTY:

```bash
git clone https://github.com/PauloTavaresProVision/ucall-transformacao.git
cd ucall-transformacao
bash instalar.sh
```

Se a pasta já existe:

```bash
cd ucall-transformacao
git pull --ff-only
bash instalar.sh
```

## Nginx

O instalador tenta ligar automaticamente a aplicação à rede de um Nginx Proxy Manager existente.

No Nginx Proxy Manager:
- Scheme: http
- Forward Hostname: ucall-transformation
- Forward Port: 8080
- Activar SSL/HTTPS no domínio escolhido.

Se o proxy não partilhar uma rede Docker, usar o IP do servidor com porta 7766. O domínio é configurado apenas no Nginx; não é necessário durante a instalação. O login de produção exige HTTPS.

## Primeira conta

Abrir o domínio HTTPS e criar a conta de Paulo. O código de instalação está no campo BOOTSTRAP_TOKEN de .env.online. A configuração inicial deixa de estar disponível depois da primeira conta. A password do PostgreSQL é gerada automaticamente.

Esta instalação leva o código completo e os dados iniciais do mandato. Os utilizadores, anexos e valores entretanto gravados no PC não são enviados no repositório. A transferência desses dados exige uma migração da base e do volume de ficheiros e chaves.

## Preservação e actualizações

.env.online e os volumes são preservados quando se repete instalar.sh. Não apagar os volumes. Guardar a base e o volume app_data, que contém anexos e chaves de cifragem.

Uma instalação anteriormente configurada com .env.contabo e compose.contabo.yaml é actualizada mantendo a ligação e os dados existentes.

## Diagnóstico

```bash
docker logs --tail 80 ucall-transformation
docker compose --env-file .env.online -f compose.online.yaml ps
```

## Estado da aplicação

Acções, processos, KPIs, reuniões, decisões, Gate, reporte, utilizadores, alertas e configuração SMTP/OpenAI estão incluídos. A submissão de pedidos de desenvolvimento está incluída. O custeio e as fases seguintes do pipeline, bem como o áudio de reuniões, ainda estão por implementar.

Frontend compilado. A instalação real na Contabo ainda tem de ser executada; não existe acesso remoto ao servidor nesta sessão.

## Pedidos de desenvolvimento

Depois de actualizar, abrir **Pedidos de desenvolvimento** no painel e seleccionar **Copiar link para as áreas**. O formulário fica em `/pedidos/novo`, no mesmo domínio HTTPS, e não exige sessão no painel.

O formulário inclui identificação, processo/actividades, esforço/custos actuais e melhoria esperada; permite guardar um rascunho neste dispositivo e rever antes de enviar. As áreas são DP, DMKT, DMC, GGP, GCGR, DLP, DSI, DFC, GDN, Academia, DC e GIC.

As horas mensais resultam de pessoas × horas por execução × execuções mensais. Custos anuais e trimestrais são convertidos para mês; custos pontuais ficam separados. O benefício esperado é uma estimativa, sem conversão automática de horas em dinheiro.

Os pedidos entram como **Submetido**, com referência e data automáticas. A equipa autenticada (líder, officers e CE) pode pesquisar e consultar os pedidos; quem abre o link não pode consultar os pedidos existentes. A API valida os dados, protege a submissão com CSRF e limita pedidos repetidos. A tabela é criada automaticamente ao arrancar, preservando os restantes dados e volumes.

Esta entrega não inclui o custeio, aprovação, implementação ou verificação pela equipa. A submissão não autoriza uma iniciativa nem altera o Gate.

### Verificação

Compilação do frontend e da API concluídas. Testes de validação, cálculos, persistência e unicidade passaram, assim como o percurso dos quatro passos no browser com API simulada e adaptação a desktop/tablet/telemóvel. O teste HTTP completo ficou bloqueado pelas permissões de ligação local desta sessão.

```bash
npm run build --prefix frontend
dotnet build api/Ucall.Api.csproj
dotnet run --project tests/Requests.Checks/Requests.Checks.csproj
```

O teste `tests/requests-ui.cjs` usa Playwright (`UCALL_PLAYWRIGHT_MODULE` permite indicar o módulo). `tests/requests.cjs` é exclusivamente para uma instalação de teste nova, com `TEST_URL` e token inicial `requests-test-only-2026`; nunca executar na instalação real.

### Enquadramento interno

Operação facturada, utilização de IA e tratamento de dados pessoais são avaliados pela Equipa de Transformação na ficha interna do pedido. Estes campos não são pedidos ao solicitante nem aceites no modelo público de submissão. Rascunhos antigos descartam estes três campos ao serem recuperados.

Cada ponto começa como **Por avaliar**, em vez de assumir Não. Líder e officers podem guardar o enquadramento; a CE pode consultar. São registados o autor e a data da última alteração. A versão impede sobrepor alterações concorrentes. Os valores anteriormente assinalados pelos solicitantes não são tratados como avaliação da equipa; os pedidos existentes ficam por avaliar até à análise interna.

A tabela de enquadramento é criada automaticamente no arranque, preservando os pedidos. A avaliação não altera o estado do pedido nem autoriza a implementação. Validação de papéis, gravação, valores pendentes e conflito de versão cobertos nos testes de servidor; formulário público e edição/consulta interna verificados no browser com API simulada.
