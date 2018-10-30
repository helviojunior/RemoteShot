# RemoteShot

## Utilização
camcontrol.exe teste.txt 192.168.15.10 8080 dominio\helvio 123456

Onde:
* teste.txt: Arquivo com a listagem dos IPs para captura da imagem das webcams
* 192.168.15.10: IP local/reverso
* 8080: Porta do reverso
* dominio\helvio: Domínio e usuário de autenticação na maquina remota
* 123456: Senha de autenticação na maquina remota

O IP e porta local para reverso são utilizados para que a maquina remota possa realizar requisições via protocolo HTTP na maquina local.
