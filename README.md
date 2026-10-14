# UCALL Transformação

Plataforma da Equipa de Transformação UCALL. React, TypeScript e Vite; ASP.NET Core 8, Identity e PostgreSQL. Registo directo pelos donos, evidências, decisões e reporte à CE.

## Instalar na Contabo

O servidor deve ter Docker, Docker Compose v2, curl, um PostgreSQL existente em Docker e Nginx ou Nginx Proxy Manager.

```bash
git clone https://github.com/PauloTavaresProVision/ucall-transformacao.git
cd ucall-transformacao
bash instalar.sh
```

O instalador pede o domínio, nome do contentor PostgreSQL, utilizador administrador e rede Docker do Nginx. Se o Nginx corre no próprio servidor, deixar a rede em branco. Cria uma base e utilizador dedicados no PostgreSQL existente, gera as credenciais, guarda a configuração com permissões restritas e constrói a aplicação.

É uma instalação nova. Não importa as contas, anexos ou medições do PC. Para migrar esses dados é necessário transferir a base dedicada e o volume com anexos e chaves de cifragem.

A criação da base usa psql dentro do contentor existente, com o utilizador administrador indicado. Esse acesso local tem de estar autorizado. Não se altera uma base ou utilizador ucall_transformation já existente.

## Encaminhamento no Nginx

- Nginx no servidor: HTTP para 127.0.0.1, porta 8088.
- Nginx em Docker na rede indicada ao instalar: HTTP para ucall-transformation, porta 8080.
- Associar o domínio escolhido e activar certificado HTTPS.

A publicação directa é limitada a 127.0.0.1:8088; não há uma nova porta pública da aplicação. HTTPS é necessário para o login em produção.

## Primeira conta

Abrir o domínio HTTPS. Criar a conta de Paulo com o código de instalação guardado em .env.contabo, campo BOOTSTRAP_TOKEN. Depois criar os restantes utilizadores na plataforma. O código de instalação deixa de permitir novas contas depois da primeira configuração.

SMTP e OpenAI são configurados na interface; as chaves são cifradas. Não colocar credenciais no GitHub.

## Actualizar

```bash
git pull --ff-only
bash instalar.sh
```

A configuração e o volume app_data são preservados. Guardar uma cópia da base dedicada e do volume antes de actualizar. Não executar docker compose down -v.

## Diagnóstico

```bash
docker logs --tail 80 ucall-transformation
curl -f http://127.0.0.1:8088/api/health
```

## Funcionalidades

Acções com dono e prazo, evidências, sub-tarefas, comentários e histórico; reuniões e decisões; processos; KPIs medidos e calculados; Gate; reporte mensal; gestão de utilizadores; alertas e teste SMTP; área privada de Paulo.

Áudio de reuniões, identificação de vozes, actas automáticas e pipeline de iniciativas ainda não estão implementados nesta versão.

## Validação

Frontend compilado no ambiente Linux. Instalador verificado com bash e simulação de serviços para instalação inicial, actualização e tratamento de erros. Não foi executado um deployment real na Contabo nesta sessão. A API conserva a versão já compilada e testada no desenvolvimento local.
