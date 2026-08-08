## Архитектура UI: Зоны и разметка

Структура страницы разделена на две главные колонки: **Левую панель (Sidebar)** и **Правую панель (Чат-зона)**.

```
+-----------------------------------------------------------------------+
| MainPage (ContentPage)                                                |
| +------------------------+------------------------------------------+ |
| | Grid.Column="0"        | Grid.Column="1"                          | |
| | (Левая панель 280px)   | (Правая панель - Чат)                    | |
| |                        | +--------------------------------------+ | |
| | - История чатов        | | Row 0: Шапка (Модель / Контекст)      | | |
| | - Выбор агентов        | +--------------------------------------+ | |
| | - Настройки            | | Row 1: MessagesCollectionView        | | |
| |                        | |        (Область сообщений)           | | |
| |                        | +--------------------------------------+ | |
| |                        | | Row 2: Поле ввода + Кнопка "Send"    | | |
| |                        | +--------------------------------------+ | |
| +------------------------+------------------------------------------+ |
+-----------------------------------------------------------------------+
```

### Разбор зон и куда что вставлять:

**Корневой** `**Grid**` **(Колонки):**

`**ColumnDefinitions="280, *"**` — определяет ширину левого сайдбара (280px) и правой рабочей области (занимает всё оставшееся пространство `*`).

**Левая панель (**`**Grid.Column="0"**`**):**

Содержит историю сессий, список чатов, переключатель локальных/облачных моделей и системных промптов.

**Правая панель (**`**Grid.Column="1"**`**):**

Внутри правой колонки находится вложенная сетка `**Grid**` с тремя строками (`**RowDefinitions="Auto, *, Auto"**`):

`**Grid.Row="0"**` **(Auto):** Верхний бар (Header) — имя текущей сессии, статус модели, выбор температуры/параметров.

`**Grid.Row="1"**` **(**`*****`**):** `**CollectionView x:Name="MessagesCollectionView"**` — главная область вывода диалога. Имя `x:Name` позволяет подвязаться к компоненту из `MainPage.xaml.cs` и осуществлять автоматическую прокрутку вниз без лагов (автоскролл).

`**Grid.Row="2"**` **(Auto):** Нижний бар (Footer) — редактор текста (`Editor`), кнопки загрузки файлов, сброса контекста и отправки запроса.

## Полный файл `MainPage.xaml`

Ниже приведен готовый файл `**MainPage.xaml**` с добавленным именем `x:Name="MessagesCollectionView"`, полноценным шаблоном сообщений и стилизованной структурой интерфейса:

```
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:SyrlasStudio.ViewModels"
             xmlns:models="clr-namespace:SyrlasStudio.Models"
             x:Class="SyrlasStudio.MainPage"
             x:DataType="vm:MainPageViewModel"
             Title="Syrlas Studio"
             BackgroundColor="{AppThemeBinding Light=#F8F9FA, Dark=#1E1E1E}">

    <!-- Корневая сетка: Левая панель (280) и Правая зона диалога (*) -->
    <Grid ColumnDefinitions="280, *">

        <!-- ========================================== -->
        <!-- 1. ЛЕВАЯ ПАНЕЛЬ: Сайдбар / История чатов   -->
        <!-- ========================================== -->
        <Border Grid.Column="0"
                BackgroundColor="{AppThemeBinding Light=#F0F2F5, Dark=#252526}"
                StrokeThickness="0"
                StrokeShape="Rectangle">
            <Grid RowDefinitions="Auto, *, Auto" Padding="12">
                <!-- Верхняя кнопка: Новый чат -->
                <Button Grid.Row="0"
                        Text="+ Новый чат"
                        Command="{Binding NewChatCommand}"
                        Margin="0,0,0,12"
                        BackgroundColor="#2B2D30"
                        TextColor="White"
                        CornerRadius="8" />

                <!-- Список диалогов -->
                <CollectionView Grid.Row="1"
                                ItemsSource="{Binding ChatSessions}"
                                SelectionMode="Single"
                                SelectedItem="{Binding SelectedSession}">
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="models:ChatSession">
                            <Grid Padding="10,8" Margin="0,2">
                                <Label Text="{Binding Title}"
                                       FontSize="14"
                                       MaxLines="1"
                                       LineBreakMode="TailTruncation"
                                       VerticalOptions="Center" />
                            </Grid>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>

                <!-- Статус локального сервера / настройки -->
                <StackLayout Grid.Row="2" Padding="0,10,0,0">
                    <Label Text="Syrlas Core: Active" 
                           FontSize="12" 
                           TextColor="Gray" 
                           HorizontalOptions="Center" />
                </StackLayout>
            </Grid>
        </Border>

        <!-- ========================================== -->
        <!-- 2. ПРАВАЯ ПАНЕЛЬ: Рабочая область чата     -->
        <!-- ========================================== -->
        <Grid Grid.Column="1" RowDefinitions="Auto, *, Auto">

            <!-- 2.1 Хедер правой панели (Grid.Row="0") -->
            <Border Grid.Row="0" 
                    Padding="16,12" 
                    BackgroundColor="{AppThemeBinding Light=#FFFFFF, Dark=#2D2D2D}"
                    StrokeThickness="1"
                    Stroke="{AppThemeBinding Light=#E0E0E0, Dark=#3E3E42}">
                <Grid ColumnDefinitions="*, Auto">
                    <StackLayout Grid.Column="0" Orientation="Vertical">
                        <Label Text="{Binding CurrentSessionTitle, DefaultValue='Ассистент разработчика'}"
                               FontAttributes="Bold"
                               FontSize="16" />
                        <Label Text="Модель: Qwen 2.5 Coder | Local Engine"
                               FontSize="12"
                               TextColor="Gray" />
                    </StackLayout>

                    <Button Grid.Column="1"
                            Text="Очистить"
                            Command="{Binding ClearMessagesCommand}"
                            FontSize="12"
                            HeightRequest="36" />
                </Grid>
            </Border>

            <!-- 2.2 Список сообщений (Grid.Row="1") -->
            <!-- Присвоено имя x:Name="MessagesCollectionView" для автоскролла из C# -->
            <CollectionView x:Name="MessagesCollectionView"
                            Grid.Row="1"
                            ItemsSource="{Binding Messages}"
                            SelectionMode="None"
                            Padding="16">
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:ChatMessage">
                        <Grid Padding="0,6">
                            <!-- Пузырь сообщения -->
                            <Border HorizontalOptions="{Binding IsUser, Converter={StaticResource AlignmentConverter}, DefaultValue='Start'}"
                                    BackgroundColor="{Binding IsUser, Converter={StaticResource MessageBgConverter}, DefaultValue='#2B2D30'}"
                                    StrokeThickness="0"
                                    MaximumWidthRequest="750"
                                    Padding="14,10">
                                <Border.StrokeShape>
                                    <RoundRectangle CornerRadius="12" />
                                </Border.StrokeShape>

                                <StackLayout Spacing="4">
                                    <Label Text="{Binding SenderName}"
                                           FontSize="11"
                                           FontAttributes="Bold"
                                           TextColor="Gray" />
                                    <Label Text="{Binding Text}"
                                           FontSize="14"
                                           LineBreakMode="WordWrap" />
                                </StackLayout>
                            </Border>
                        </Grid>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <!-- 2.3 Панель ввода текста (Grid.Row="2") -->
            <Border Grid.Row="2"
                    Padding="12"
                    BackgroundColor="{AppThemeBinding Light=#FFFFFF, Dark=#2D2D2D}"
                    StrokeThickness="1"
                    Stroke="{AppThemeBinding Light=#E0E0E0, Dark=#3E3E42}">
                <Grid ColumnDefinitions="*, Auto" ColumnSpacing="10">
                    <Editor Grid.Column="0"
                            Text="{Binding InputText, Mode=TwoWay}"
                            Placeholder="Введите запрос или промпт..."
                            AutoSize="TextChanges"
                            MaxHeightRequest="150"
                            FontSize="14" />

                    <Button Grid.Column="1"
                            Text="Отправить"
                            Command="{Binding SendMessageCommand}"
                            VerticalOptions="End"
                            HeightRequest="40"
                            Padding="16,0" />
                </Grid>
            </Border>

        </Grid>

    </Grid>
</ContentPage>
```