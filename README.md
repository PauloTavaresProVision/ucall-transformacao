# UCALL Transformação

## Estado do repositório

O instalador para Contabo está disponível. O código da aplicação ainda não foi transferido para este repositório. Não arrancar a instalação até existirem Dockerfile, api/Ucall.Api.csproj e frontend/package.json. O instalador verifica este requisito antes de alterar a base de dados.

## Instalação

Com o código completo no repositório:

```bash
git clone https://github.com/PauloTavaresProVision/ucall-transformacao.git
cd ucall-transformacao
bash instalar.sh
```

Requisitos: Linux, Docker e Compose v2 a funcionar, PostgreSQL existente num contentor ligado a uma rede Docker com nome, acesso administrativo ao PostgreSQL por docker exec e Nginx existente. A porta local 8088 e o nome de contentor ucall-transformation devem estar livres.

O instalador pede o domínio, contentor PostgreSQL, utilizador administrador e rede do Nginx. Cria uma base e utilizador dedicados no PostgreSQL existente, gera credenciais e o token de instalação, guarda a configuração em .env.contabo com permissões restritas e constrói a aplicação. Não altera uma base ou utilizador ucall_transformation já existente; nesse caso é necessário configurar a ligação manualmente. Trata-se de uma instalação nova, sem migração dos dados do PC.

Nginx no servidor: encaminhar para http://127.0.0.1:8088. Nginx em Docker na rede indicada ao instalar: encaminhar para http://ucall-transformation:8080. Activar HTTPS no domínio. A aplicação usa cookies seguros em produção.

O token para criar a primeira conta está em .env.contabo, campo BOOTSTRAP_TOKEN. Não publicar este ficheiro, passwords ou chaves no GitHub.

## Actualizações

```bash
git pull --ff-only
bash instalar.sh
```

Manter a configuração e os volumes entre actualizações. Antes de actualizar, guardar uma cópia da base dedicada e do volume app_data, que inclui anexos e chaves necessárias para ler configurações cifradas.

## Validação pendente

O instalador ainda não foi executado num servidor Linux. A instalação completa será validada depois da publicação do código da aplicação.
