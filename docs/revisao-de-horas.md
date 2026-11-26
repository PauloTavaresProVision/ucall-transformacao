# Revisão de horas dos pedidos
Especificação: email da Inês de 25/11/2026 e respectivo PDF. O portal está em `/pedidos/meus`; a Equipa de Transformação acompanha a revisão em **Pedidos de desenvolvimento → Revisão de horas**. O formulário de novos pedidos mantém o seu endereço.

## Antes de abrir às áreas
1. Fazer e verificar o backup da base dedicada e do volume de chaves. Publicar em HTTPS. No ficheiro de ambiente usado com compose.online.yaml definir `UCALL_PUBLIC_URL=https://<dns-interno>`. Configurar e testar o SMTP em Configurações.
2. A actualização cria as tabelas de revisão sem sobrepor o conteúdo das submissões. A lista contém 35 linhas (P1: 7, P2: 5, P3: 23). A ligação inicial exige área e referência confirmadas, ou correspondência única por área e título normalizado; um título semelhante não basta. O líder resolve pendências em **Validar responsáveis, triagem e acessos de direcção**. Não usar os valores ilustrativos de horas do PDF como dados reais.
3. Confirmar o email individual de cada responsável antes de guardar a atribuição. Emails genéricos partilhados não identificam uma pessoa. Autorizar explicitamente cada director ou adjunto por email e área. Um email não obtém acesso por pertencer ao mesmo domínio ou por conhecer uma referência. Uma alteração ou revogação de atribuição aplica-se também às sessões em curso.
4. O teste salarial e o duplicado das férias não aparecem nos pedidos. Para os eliminar fisicamente após verificar o backup, aplicar `sql/002-review-cleanup.sql` na base PostgreSQL dedicada. A transacção verifica o registo a conservar e as áreas. Não apaga por título.
5. Validar com dois responsáveis da mesma área e um director; depois partilhar o endereço do portal. Os códigos duram dez minutos, são de utilização única e têm cinco tentativas. A sessão da área é separada do painel interno.
6. Em **Revisão de horas** filtrar por estado, área e prioridade. Os lembretes são manuais, só para destinatários com pedidos em falta e no máximo um por endereço em 24 horas. O XLSX contém estado, horas, pressupostos, detalhe por nível e totais de revisões submetidas. Não inclui custo-hora por nível.

As revisões têm histórico de autor, data e valores. O original permanece disponível em leitura. Revisões submetidas podem ser corrigidas com novo registo de auditoria, sem voltar a rascunho. Guardar com uma versão antiga devolve conflito.

## Verificação técnica
Na raiz: `dotnet run --project tests/Review.Checks` testa os endpoints com SQLite e correio simulado, sem abrir portas. Para o teste de transporte com SMTP local, executar `dotnet build api/Ucall.Api.csproj` e `python tests/review-integration.py`. Para o percurso de interface, compilar o frontend e executar `node tests/review-ui.cjs` com Playwright e Chromium disponíveis. O teste abre o XLSX e verifica os valores.
