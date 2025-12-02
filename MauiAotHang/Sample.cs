using GeneratedClasses;

namespace MauiAotHang;

public static class Sample
{
    public static Task<int> Run() 
    {
        return Task.FromResult(42);
    }

    // public static async Task<int> Run()
    // {
    //     Console.WriteLine("Running Sample");
    //     var res = await Caller.All<int>(42);
    //     Console.WriteLine("Finished Generic Caller");
    //     var res2 = Caller.AllTypes0(42);
    //     var res3 = Caller.AllTypes1(42);
    //     var res4 = Caller.AllTypes2(42);
    //     var res5 = Caller.AllTypes3(42);
    //     var res6 = Caller.AllTypes4(42);
    //     var res7 = Caller.AllTypes5(42);
    //     var res8 = Caller.AllTypes6(42);
    //     var res9 = Caller.AllTypes7(42);
    //     var res10 = Caller.AllTypes8(42);
    //     var res11 = Caller.AllTypes9(42);
    //     var res12 = Caller.AllTypes10(42);
    //     var res13 = Caller.AllTypes11(42);
    //     var res14 = Caller.AllTypes12(42);
    //     var res15 = Caller.AllTypes13(42);
    //     var res16 = Caller.AllTypes14(42);
    //     var res17 = Caller.AllTypes15(42);
    //     var res18 = Caller.AllTypes16(42);
    //     var res19 = Caller.AllTypes17(42);
    //     var res20 = Caller.AllTypes18(42);
    //     var res21 = Caller.AllTypes19(42);
    //     var res22 = Caller.AllTypes20(42);
    //     var res23 = Caller.AllTypes21(42);
    //     var res24 = Caller.AllTypes22(42);
    //     var res25 = Caller.AllTypes23(42);
    //     var res26 = Caller.AllTypes24(42);
    //     var res27 = Caller.AllTypes25(42);
    //     var res28 = Caller.AllTypes26(42);
    //     var res29 = Caller.AllTypes27(42);
    //     var res30 = Caller.AllTypes28(42);
    //     var res31 = Caller.AllTypes29(42);
    //     var res32 = Caller.AllTypes30(42);
    //     var res33 = Caller.AllTypes31(42);
    //     var res34 = Caller.AllTypes32(42);
    //     var res35 = Caller.AllTypes33(42);
    //     var res36 = Caller.AllTypes34(42);
    //     var res37 = Caller.AllTypes35(42);
    //     var res38 = Caller.AllTypes36(42);
    //     var res39 = Caller.AllTypes37(42);
    //     var res40 = Caller.AllTypes38(42);
    //     var res41 = Caller.AllTypes39(42);
    //     var res42 = Caller.AllTypes40(42);
    //     var res43 = Caller.AllTypes41(42);
    //     var res44 = Caller.AllTypes42(42);
    //     var res45 = Caller.AllTypes43(42);
    //     var res46 = Caller.AllTypes44(42);
    //     var res47 = Caller.AllTypes45(42);
    //     var res48 = Caller.AllTypes46(42);
    //     var res49 = Caller.AllTypes47(42);
    //     var res50 = Caller.AllTypes48(42);
    //     var res51 = Caller.AllTypes49(42);
    //     var res52 = Caller.AllTypes50(42);
    //     var res53 = Caller.AllTypes51(42);
    //     var res54 = Caller.AllTypes52(42);
    //     var res55 = Caller.AllTypes53(42);
    //     var res56 = Caller.AllTypes54(42);
    //     var res57 = Caller.AllTypes55(42);
    //     var res58 = Caller.AllTypes56(42);
    //     var res59 = Caller.AllTypes57(42);
    //     var res60 = Caller.AllTypes58(42);
    //     var res61 = Caller.AllTypes59(42);
    //     var res62 = Caller.AllTypes60(42);
    //     var res63 = Caller.AllTypes61(42);
    //     var res64 = Caller.AllTypes62(42);
    //     var res65 = Caller.AllTypes63(42);
    //     var res66 = Caller.AllTypes64(42);
    //     var res67 = Caller.AllTypes65(42);
    //     var res68 = Caller.AllTypes66(42);
    //     var res69 = Caller.AllTypes67(42);
    //     var res70 = Caller.AllTypes68(42);
    //     var res71 = Caller.AllTypes69(42);
    //     var res72 = Caller.AllTypes70(42);
    //     var res73 = Caller.AllTypes71(42);
    //     var res74 = Caller.AllTypes72(42);
    //     var res75 = Caller.AllTypes73(42);
    //     var res76 = Caller.AllTypes74(42);
    //     var res77 = Caller.AllTypes75(42);
    //     var res78 = Caller.AllTypes76(42);
    //     var res79 = Caller.AllTypes77(42);
    //     var res80 = Caller.AllTypes78(42);
    //     var res81 = Caller.AllTypes79(42);
    //     var res82 = Caller.AllTypes80(42);
    //     var res83 = Caller.AllTypes81(42);
    //     var res84 = Caller.AllTypes82(42);
    //     var res85 = Caller.AllTypes83(42);
    //     var res86 = Caller.AllTypes84(42);
    //     var res87 = Caller.AllTypes85(42);
    //     var res88 = Caller.AllTypes86(42);
    //     var res89 = Caller.AllTypes87(42);
    //     var res90 = Caller.AllTypes88(42);
    //     var res91 = Caller.AllTypes89(42);
    //     var res92 = Caller.AllTypes90(42);
    //     var res93 = Caller.AllTypes91(42);
    //     var res94 = Caller.AllTypes92(42);
    //     var res95 = Caller.AllTypes93(42);
    //     var res96 = Caller.AllTypes94(42);
    //     var res97 = Caller.AllTypes95(42);
    //     var res98 = Caller.AllTypes96(42);
    //     var res99 = Caller.AllTypes97(42);
    //     var res100 = Caller.AllTypes98(42);
    //     var res101 = Caller.AllTypes99(42);
    //     Console.WriteLine("Finished All Types Callers");
    //     var res102 = Caller.AllTypesLarge(42);
    //     Console.WriteLine("Finished All Types Large Caller");
    //     return res.First() + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10 + res11 + res12 + res13 + res14 + res15 + res16 + res17 + res18 + res19 + res20 + res21 + res22 + res23 + res24 + res25 + res26 + res27 + res28 + res29 + res30 + res31 + res32 + res33 + res34 + res35 + res36 + res37 + res38 + res39 + res40 + res41 + res42 + res43 + res44 + res45 + res46 + res47 + res48 + res49 + res50 + res51 + res52 + res53 + res54 + res55 + res56 + res57 + res58 + res59 + res60 + res61 + res62 + res63 + res64 + res65 + res66 + res67 + res68 + res69 + res70 + res71 + res72 + res73 + res74 + res75 + res76 + res77 + res78 + res79 + res80 + res81 + res82 + res83 + res84 + res85 + res86 + res87 + res88 + res89 + res90 + res91 + res92 + res93 + res94 + res95 + res96 + res97 + res98 + res99 + res100 + res101 + res102;
    // }
}