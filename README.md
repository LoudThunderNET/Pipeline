## Что перед Вами ?
Перед Вами библиотека, реализующая идею конвейерного программирования, высказанную Скоттом Влашиным (Scott Wlaschin) в докладе "[Pipeline-oriented programming](https://www.youtube.com/watch?v=ipceTuJlw-M)" .
Ключевые моменты идеи:
- код разбивается на небольшие, независимые фрагменты кода. Эти фрагменты, в парадигме конвейерного программирования, называются пайпы (Pipe);
- пайпы объединяются в конвейер.

Пайпы обладают следующими свойствами:
- пайпы реализуют простой интерфейс;
- пайпы легко комбинировать между собой и выстраивать логику приложения, словно строим дом из кирпичей;
- пайпы легко тестировать и сопровождать благодаря своему малому размеру логики;
- пайпы "взаимодействуют" между собой посредством контекста коневейера.
  
## Реализация идеи.
В библиотеке определены следующие абстракции:
- `IPipe<TRequest, TResponse>` - определяет пайп. Клиентский код ;
- `PipelineContext<TRequest, TResponse>` - контекст конвейера;
- `PipelineDefinition<TRequest, TResponse>` - абстрактный класс определения конвейера. Предоставляет API конструирования конвейера обработки из пользовательских и встроенных пайпов.

## Использование.
Определяем классы запроса/ответа для каждого конвейера. Да, Вы можете определить несколько конвейеров в коде. Конвейеры различаются параметрами обощенных типов TRequest и TResponse для `IPipe<TRequest, TResponse>`.
Реализуем интерфейс `IPipe<TRequest, TResponse>` классом пайпа бизнес логики. 
```csharp
internal class ValidationPipe(ILogger<ValidationPipe> logger, IPipe<Request, Response> next) 
    : IPipe<Request, Response>
{
    public Task HandleAsync(PipelineContext<Request, Response> context)
    {
        logger.LogInformation($"{nameof(ValidationPipe)} called");
        context.Valid(true);

        return next.HandleAsync(context);
    }
}
```
Чnобы пайп мог быть использован в любом месте конвейера, его конструктор должен иметь зависимость типа `IPipe<TRequest, TResponse>`, но только одну. Более 1 зависимости типа `IPipe<TRequest, TResponse>` вызовет исключение во время регистрации зависимостей.

Создаем определение конвейера, путем создания класса, производного от `PipelineDefinition<TRequest, TResponse>`
```csharp
public class SamplePipelineDefinition : PipelineDefinition<Request, Response>
{
    protected override void Define()
    {
        Pipeline()
            .AddPipe<ValidationPipe>()
            .Alter(
              ctx => ctx.IsValid() == true,
              pipe => pipe.AddPipe<NotifyInvalidRequest>())
            .AddPipe<GenerateDocuments>();
    }
}
```
Регистриуем наши конвейеры в контейнере DI
```csharp
internal class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddPipelines(typeof(SamplePipelineDefinition).Assembly); // один вызов регистриует все конвейеры в сборке.
        // регистрация других зависимостей.

        var serviceProvider = services.BuildServiceProvider();
        using var asyncScope = serviceProvider.CreateAsyncScope();

        var pipeline = asyncScope.ServiceProvider.GetRequiredService<IPipeline<Request, Response>>();

        var response = await pipeline.HandleAsync(new Request 
        { 
            Id = 123,
            Name = "Hello world"
        }, default);

        Console.WriteLine(response.Message);
    }
}
```

## API определения конвейера.
Определение конвейера реализуется путем переопределения метода `Define()` класса `PipelineDefinition<Request, Response>`. Определение должно начинаться в вызова метода `Pipeline()`, который возвращает строитель конвейра. Строитель конвейера предоставляет следующие методы API:
- `AddPipe<TPipeType>()` - добавлет в конвейер пайп.
- `If(Predicate<PipelineContext<TRequest, TResponse>> predicate, Action<PipelineBuilder> positiveBranch)` - добавляет пайп условного выполнения ветки конвейера.
- `IfElse(Predicate<PipelineContext<TRequest, TResponse>> predicate,
      Action<PipelineBuilder> positiveBranch,
      Action<PipelineBuilder> altBranch)` - добавляет пайп условного выполнения двух ветвей конвейера. Если условия выполняется, то исполняется позитивня ветвь конвейра, иначе альтернативная ветвь. После исполнения условных ветвей, продолжается выполненеи основного конвейера.
- `Alter(Predicate<PipelineContext<TRequest, TResponse>> predicate,
        Action<PipelineBuilder> positiveBranch)`  - добавляет пайп условного ветвления основного конвейра. Если условие выполняется, то исполнение кода продолжиться на позитивной ветки конвейера без возврата к основному. Иначе - продолжиться выполнение основго ветки конвейера.
Условные пайпы могут вкладываться друг в друга, образуя сложные ветвления. Степень ветвления и сложность остается на Ваше усмотрение.

Ниже приведена иллюстрация, поясняющая работу условных пайпов.

![ConditionalPipes](https://github.com/user-attachments/assets/e1b46861-d135-4864-bf5b-544a0ac275bd)

## Контекст конвейера

Контекст конвейера представлен обощенным классом `PipelineContext<TRequest, TResponse>`. Он влючает три свойства: 
- `Request` - хранит исходный запрос для конвейра;
- `Response` - хранит результат работы конвейера;
- `Properties` - словарь значений. Ключ словаря - строка, значение тип `PropertyValue`.
Класс `PropertyValue` предоставляет свойств различного типа, что позволяет избежать операция упаковки/распаковки для значимых типов. Для того чтобы свойством `Properties` было удобно и безопасно пользоваться, рекомендуется создать класс с методами расширения типа `PipelineContext<TRequest, TResponse>`, позволяющие записывать и считывать промежуточные данные. Эти данные пайпы могут использовать для взаимодействия между собой.
