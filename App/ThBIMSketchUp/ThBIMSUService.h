#pragma once

class ThBIMSUService final
{
public:
    static ThBIMSUService& GetInstance();

private:
    ThBIMSUService() = default;
    ~ThBIMSUService() = default;

    ThBIMSUService(ThBIMSUService&&) = delete;
    ThBIMSUService(const ThBIMSUService&) = delete;
    ThBIMSUService& operator=(ThBIMSUService&&) = delete;
    ThBIMSUService& operator=(const ThBIMSUService&) = delete;

public:
    void Terminate();
    void Initialize();
};

ThBIMSUService& ThBIMSUService::GetInstance()
{
    static ThBIMSUService instance;
    return instance;
}