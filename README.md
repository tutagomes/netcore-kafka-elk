
Para executar a stack:

> É recomendado executar uma a uma, para que haja tempo de baixar as imagens, subir os contêineres e configurar cada parte!

```sh

    docker-compose -f kafka-compose.yaml up -d

    docker-compose -f elk-compose.yaml up -d

    docker-compose  -f app-compose.yaml up

```

Para publicar uma nova mensagem no bus, basta mandar um POST para a URL do Iniciador, que deve ser http://localhost:5001

Assim, será criada uma mensagem aleatória (que pode ser conferida no arquivo Iniciador/Controllers/HomeController.cs) e publicada no cluster Kafka presente.

Por padrão, o tópico é criado automaticamente. Caso queira visualizar as mensagens e os tópicos, basta acessar a interface disponível em http://localhost:8080/ui/clusters/local/topics

E para o Kibana, http://localhost:5601
