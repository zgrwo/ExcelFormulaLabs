using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
#pragma warning disable CS8625 // null literal for UDF null-input testing
using Xunit;

namespace FormulaLabs.DataToolkit.Tests
{
    public class ArrayUdfTests
    {
        // ══════════════════════════════════════════════════════════════════
        //  ARR.SORT  (A(a) → Sort with asc+mode)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Sort_asc() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{3,1,2},true,"auto"); r.Should().Equal(1,2,3); }
        [Fact] public void Sort_desc() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{1,2,3},false,"auto"); r.Should().Equal(3,2,1); }
        [Fact] public void Sort_numeric() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{30,5,200},true,"numeric"); r.Should().Equal(5,30,200); }
        [Fact] public void Sort_text_integers() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{10,2,1},true,"text"); r.Should().Equal(1,10,2); }
        [Fact] public void Sort_null() { var r=(object[])ArrayUdf.UDF_ARR_SORT(null!,true,"auto"); r.Should().BeEmpty(); }
        [Fact] public void Sort_empty() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[0],true,"auto"); r.Should().BeEmpty(); }
        [Fact] public void Sort_single() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{42},true,"auto"); r.Should().Equal(42); }
        [Fact] public void Sort_strings_asc() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{"c","a","b"},true,"text"); r.Should().Equal("a","b","c"); }
        [Fact] public void Sort_strings_desc() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{"a","b","c"},false,"text"); r.Should().Equal("c","b","a"); }
        [Fact] public void Sort_already_sorted() { var r=(object[])ArrayUdf.UDF_ARR_SORT(new object[]{1,2,3,4},true,"auto"); r.Should().Equal(1,2,3,4); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SORTASC  (A(a) → Sort asc)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SortAsc_basic() { var r=(object[])ArrayUdf.UDF_ARR_SORTASC(new object[]{3,1,2}); r.Should().Equal(1,2,3); }
        [Fact] public void SortAsc_null() { var r=(object[])ArrayUdf.UDF_ARR_SORTASC(null!); r.Should().BeEmpty(); }
        [Fact] public void SortAsc_empty() { var r=(object[])ArrayUdf.UDF_ARR_SORTASC(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void SortAsc_strings() { var r=(object[])ArrayUdf.UDF_ARR_SORTASC(new object[]{"z","a","m"}); r.Should().Equal("a","m","z"); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SORTDESC  (A(a) → Sort desc)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SortDesc_basic() { var r=(object[])ArrayUdf.UDF_ARR_SORTDESC(new object[]{1,2,3}); r.Should().Equal(3,2,1); }
        [Fact] public void SortDesc_null() { var r=(object[])ArrayUdf.UDF_ARR_SORTDESC(null!); r.Should().BeEmpty(); }
        [Fact] public void SortDesc_empty() { var r=(object[])ArrayUdf.UDF_ARR_SORTDESC(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void SortDesc_strings() { var r=(object[])ArrayUdf.UDF_ARR_SORTDESC(new object[]{"a","m","z"}); r.Should().Equal("z","m","a"); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SORTNUM  (A(a) → Sort numeric)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SortNum_basic() { var r=(object[])ArrayUdf.UDF_ARR_SORTNUM(new object[]{30,5,200}); r.Should().Equal(5,30,200); }
        [Fact] public void SortNum_doubles() { var r=(object[])ArrayUdf.UDF_ARR_SORTNUM(new object[]{3.5,1.1,2.2}); r.Should().Equal(1.1,2.2,3.5); }
        [Fact] public void SortNum_null() { var r=(object[])ArrayUdf.UDF_ARR_SORTNUM(null!); r.Should().BeEmpty(); }
        [Fact] public void SortNum_empty() { var r=(object[])ArrayUdf.UDF_ARR_SORTNUM(new object[0]); r.Should().BeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SORTTEXT  (A(a) → Sort text)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void SortText_basic() { var r=(object[])ArrayUdf.UDF_ARR_SORTTEXT(new object[]{10,2,1}); r.Should().Equal(1,10,2); }
        [Fact] public void SortText_strings() { var r=(object[])ArrayUdf.UDF_ARR_SORTTEXT(new object[]{"c","a","b"}); r.Should().Equal("a","b","c"); }
        [Fact] public void SortText_null() { var r=(object[])ArrayUdf.UDF_ARR_SORTTEXT(null!); r.Should().BeEmpty(); }
        [Fact] public void SortText_empty() { var r=(object[])ArrayUdf.UDF_ARR_SORTTEXT(new object[0]); r.Should().BeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.UNIQUE  (A(a) → Unique via hash-set)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Unique_basic() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[]{1,2,2,3,3,3}); r.Should().Equal(1,2,3); }
        [Fact] public void Unique_all_unique() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[]{1,2,3}); r.Should().Equal(1,2,3); }
        [Fact] public void Unique_strings() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[]{"a","b","a"}); r.Should().Equal("a","b"); }
        [Fact] public void Unique_single() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[]{42}); r.Should().Equal(42); }
        [Fact] public void Unique_null() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(null!); r.Should().BeEmpty(); }
        [Fact] public void Unique_empty() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void Unique_all_same() { var r=(object[])ArrayUdf.UDF_ARR_UNIQUE(new object[]{7,7,7,7}); r.Should().Equal(7); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.INDEXOF  (A(a), v → long)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void IndexOf_found() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[]{"a","b","c"},"b")).Should().Be(1);
        [Fact] public void IndexOf_not_found() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[]{"a","b","c"},"z")).Should().Be(-1);
        [Fact] public void IndexOf_first_occurrence() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[]{1,2,1,3},1)).Should().Be(0);
        [Fact] public void IndexOf_strings() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[]{"x","y","z"},"y")).Should().Be(1);
        [Fact] public void IndexOf_null_input() => ((long)ArrayUdf.UDF_ARR_INDEXOF(null!,"a")).Should().Be(-1);
        [Fact] public void IndexOf_empty() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[0],"a")).Should().Be(-1);
        [Fact] public void IndexOf_null_value() => ((long)ArrayUdf.UDF_ARR_INDEXOF(new object[]{"a",null,"c"},null!)).Should().Be(1);

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SLICE  (A(a), start, len → object[])
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Slice_basic() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{1,2,3,4,5},1,3); r.Should().Equal(2,3,4); }
        [Fact] public void Slice_from_start() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{1,2,3,4,5},0,2); r.Should().Equal(1,2); }
        [Fact] public void Slice_to_end() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{1,2,3,4,5},3,-1); r.Should().Equal(4,5); }
        [Fact] public void Slice_single() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{10,20,30},1,1); r.Should().Equal(20); }
        [Fact] public void Slice_null() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(null!,0,1); r.Should().BeEmpty(); }
        [Fact] public void Slice_empty() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[0],0,1); r.Should().BeEmpty(); }
        [Fact] public void Slice_strings() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{"a","b","c","d"},1,2); r.Should().Equal("b","c"); }
        [Fact] public void Slice_whole_array() { var r=(object[])ArrayUdf.UDF_ARR_SLICE(new object[]{1,2,3},0,3); r.Should().Equal(1,2,3); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FLATTEN  (NormalizeTo2D! → Flatten2D → object[])
        //  Null → NormalizeTo2D returns null → Flatten handles null → empty array
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Flatten_basic() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{1,2},{3,4}}); r.Should().Equal(1,2,3,4); }
        [Fact] public void Flatten_single_row() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{1,2,3}}); r.Should().Equal(1,2,3); }
        [Fact] public void Flatten_single_col() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{1},{2},{3}}); r.Should().Equal(1,2,3); }
        [Fact] public void Flatten_single_cell() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{42}}); r.Should().Equal(42); }
        [Fact] public void Flatten_null() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(null!); r.Should().BeEmpty(); }
        [Fact] public void Flatten_empty_2d() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[0,0]); r.Should().BeEmpty(); }
        [Fact] public void Flatten_strings() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{"a","b"},{"c","d"}}); r.Should().Equal("a","b","c","d"); }
        [Fact] public void Flatten_mixed() { var r=(object[])ArrayUdf.UDF_ARR_FLATTEN(new object[,]{{1,"x"},{3.5,"y"}}); r.Should().Equal(1,"x",3.5,"y"); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILTER  (A(a), crit, op → object[])
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Filter_eq() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{1,2,3,2,4},2,"="); r.Should().Equal(2,2); }
        [Fact] public void Filter_gt() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{1,2,3,4,5},3,">"); r.Should().Equal(4,5); }
        [Fact] public void Filter_lt() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{1,2,3,4},3,"<"); r.Should().Equal(1,2); }
        [Fact] public void Filter_ne() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{1,2,1,2},1,"<>"); r.Should().Equal(2,2); }
        [Fact] public void Filter_no_match() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{1,2,3},99,"="); r.Should().BeEmpty(); }
        [Fact] public void Filter_null() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(null!,1,"="); r.Should().BeEmpty(); }
        [Fact] public void Filter_empty() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[0],1,"="); r.Should().BeEmpty(); }
        [Fact] public void Filter_strings() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{"a","b","a"},"a","="); r.Should().Equal("a","a"); }
        [Fact] public void Filter_all_match() { var r=(object[])ArrayUdf.UDF_ARR_FILTER(new object[]{5,5,5},5,"="); r.Should().Equal(5,5,5); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILTER_EQ  (A(a), crit → Filter with "=")
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void FilterEq_basic() { var r=(object[])ArrayUdf.UDF_ARR_FEQ(new object[]{1,2,1,3},1); r.Should().Equal(1,1); }
        [Fact] public void FilterEq_no_match() { var r=(object[])ArrayUdf.UDF_ARR_FEQ(new object[]{1,2,3},99); r.Should().BeEmpty(); }
        [Fact] public void FilterEq_null() { var r=(object[])ArrayUdf.UDF_ARR_FEQ(null!,1); r.Should().BeEmpty(); }
        [Fact] public void FilterEq_strings() { var r=(object[])ArrayUdf.UDF_ARR_FEQ(new object[]{"x","y","x"},"x"); r.Should().Equal("x","x"); }
        [Fact] public void FilterEq_empty() { var r=(object[])ArrayUdf.UDF_ARR_FEQ(new object[0],1); r.Should().BeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILTER_NE  (A(a), crit → Filter with "<>")
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void FilterNe_basic() { var r=(object[])ArrayUdf.UDF_ARR_FNE(new object[]{1,2,1,3},1); r.Should().Equal(2,3); }
        [Fact] public void FilterNe_no_match() { var r=(object[])ArrayUdf.UDF_ARR_FNE(new object[]{5,5,5},5); r.Should().BeEmpty(); }
        [Fact] public void FilterNe_null() { var r=(object[])ArrayUdf.UDF_ARR_FNE(null!,1); r.Should().BeEmpty(); }
        [Fact] public void FilterNe_strings() { var r=(object[])ArrayUdf.UDF_ARR_FNE(new object[]{"a","b","a"},"a"); r.Should().Equal("b"); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILTER_GT  (A(a), crit → Filter with ">")
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void FilterGt_basic() { var r=(object[])ArrayUdf.UDF_ARR_FGT(new object[]{1,2,3,4},2); r.Should().Equal(3,4); }
        [Fact] public void FilterGt_no_match() { var r=(object[])ArrayUdf.UDF_ARR_FGT(new object[]{1,2,3},99); r.Should().BeEmpty(); }
        [Fact] public void FilterGt_null() { var r=(object[])ArrayUdf.UDF_ARR_FGT(null!,1); r.Should().BeEmpty(); }
        [Fact] public void FilterGt_all_match() { var r=(object[])ArrayUdf.UDF_ARR_FGT(new object[]{10,20,30},5); r.Should().Equal(10,20,30); }
        [Fact] public void FilterGt_empty() { var r=(object[])ArrayUdf.UDF_ARR_FGT(new object[0],1); r.Should().BeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILTER_LT  (A(a), crit → Filter with "<")
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void FilterLt_basic() { var r=(object[])ArrayUdf.UDF_ARR_FLT(new object[]{1,2,3,4},3); r.Should().Equal(1,2); }
        [Fact] public void FilterLt_no_match() { var r=(object[])ArrayUdf.UDF_ARR_FLT(new object[]{5,6,7},1); r.Should().BeEmpty(); }
        [Fact] public void FilterLt_null() { var r=(object[])ArrayUdf.UDF_ARR_FLT(null!,1); r.Should().BeEmpty(); }
        [Fact] public void FilterLt_all_match() { var r=(object[])ArrayUdf.UDF_ARR_FLT(new object[]{1,2,3},99); r.Should().Equal(1,2,3); }
        [Fact] public void FilterLt_empty() { var r=(object[])ArrayUdf.UDF_ARR_FLT(new object[0],1); r.Should().BeEmpty(); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.CONCAT  (A(a), A(b) → Concat → object[])
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Concat_basic() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[]{1,2},new object[]{3,4}); r.Should().Equal(1,2,3,4); }
        [Fact] public void Concat_first_empty() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[0],new object[]{1,2}); r.Should().Equal(1,2); }
        [Fact] public void Concat_second_empty() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[]{1,2},new object[0]); r.Should().Equal(1,2); }
        [Fact] public void Concat_both_empty() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[0],new object[0]); r.Should().BeEmpty(); }
        [Fact] public void Concat_first_null() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(null!,new object[]{1,2}); r.Should().Equal(1,2); }
        [Fact] public void Concat_second_null() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[]{1,2},null!); r.Should().Equal(1,2); }
        [Fact] public void Concat_both_null() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(null!,null!); r.Should().BeEmpty(); }
        [Fact] public void Concat_strings() { var r=(object[])ArrayUdf.UDF_ARR_CONCAT(new object[]{"a","b"},new object[]{"c"}); r.Should().Equal("a","b","c"); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.REVERSE  (A(a) → Reverse → object[])
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Reverse_basic() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[]{1,2,3}); r.Should().Equal(3,2,1); }
        [Fact] public void Reverse_single() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[]{42}); r.Should().Equal(42); }
        [Fact] public void Reverse_empty() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void Reverse_null() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(null!); r.Should().BeEmpty(); }
        [Fact] public void Reverse_strings() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[]{"a","b","c"}); r.Should().Equal("c","b","a"); }
        [Fact] public void Reverse_two_elements() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[]{1,2}); r.Should().Equal(2,1); }
        [Fact] public void Reverse_roundtrip() { var r=(object[])ArrayUdf.UDF_ARR_REVERSE(new object[]{1,2,3}); var r2=(object[])ArrayUdf.UDF_ARR_REVERSE(r); r2.Should().Equal(1,2,3); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.COUNT  (A(a) → Count → long)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Count_basic() => ((long)ArrayUdf.UDF_ARR_COUNT(new object[]{1,2,3,4,5})).Should().Be(5);
        [Fact] public void Count_single() => ((long)ArrayUdf.UDF_ARR_COUNT(new object[]{42})).Should().Be(1);
        [Fact] public void Count_empty() => ((long)ArrayUdf.UDF_ARR_COUNT(new object[0])).Should().Be(0);
        [Fact] public void Count_null() => ((long)ArrayUdf.UDF_ARR_COUNT(null!)).Should().Be(0);
        [Fact] public void Count_large() { var a=new object[1000]; for(int i=0;i<1000;i++)a[i]=i; ((long)ArrayUdf.UDF_ARR_COUNT(a)).Should().Be(1000); }
        [Fact] public void Count_strings() => ((long)ArrayUdf.UDF_ARR_COUNT(new object[]{"a","b","c"})).Should().Be(3);

        // ══════════════════════════════════════════════════════════════════
        //  ARR.CONTAINS  (A(a), v → Contains → bool)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Contains_true() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{1,2,3},2)).Should().BeTrue();
        [Fact] public void Contains_false() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{1,2,3},99)).Should().BeFalse();
        [Fact] public void Contains_first() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{1,2,3},1)).Should().BeTrue();
        [Fact] public void Contains_last() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{1,2,3},3)).Should().BeTrue();
        [Fact] public void Contains_string() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{"a","b","c"},"b")).Should().BeTrue();
        [Fact] public void Contains_null_data() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(null!,"a")).Should().BeFalse();
        [Fact] public void Contains_empty() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[0],"a")).Should().BeFalse();
        [Fact] public void Contains_null_value() => ((bool)ArrayUdf.UDF_ARR_CONTAINS(new object[]{"a",null,"c"},null!)).Should().BeTrue();

        // ══════════════════════════════════════════════════════════════════
        //  ARR.TOSET  (A(a) → Unique → object[]) — alias for UNIQUE
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void ToSet_basic() { var r=(object[])ArrayUdf.UDF_ARR_TOSET(new object[]{1,2,2,3}); r.Should().Equal(1,2,3); }
        [Fact] public void ToSet_null() { var r=(object[])ArrayUdf.UDF_ARR_TOSET(null!); r.Should().BeEmpty(); }
        [Fact] public void ToSet_strings() { var r=(object[])ArrayUdf.UDF_ARR_TOSET(new object[]{"a","b","a","c"}); r.Should().Equal("a","b","c"); }
        [Fact] public void ToSet_empty() { var r=(object[])ArrayUdf.UDF_ARR_TOSET(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void ToSet_single() { var r=(object[])ArrayUdf.UDF_ARR_TOSET(new object[]{99}); r.Should().Equal(99); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.FILL  (v, n → object[]) — toLong for count
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Fill_basic() { var r=(object[])ArrayUdf.UDF_ARR_FILL("x",3); r.Should().Equal("x","x","x"); }
        [Fact] public void Fill_numeric() { var r=(object[])ArrayUdf.UDF_ARR_FILL(42,4); r.Should().Equal(42,42,42,42); }
        [Fact] public void Fill_zero_count() { var r=(object[])ArrayUdf.UDF_ARR_FILL("x",0); r.Should().BeEmpty(); }
        [Fact] public void Fill_null_value() { var r=(object[])ArrayUdf.UDF_ARR_FILL(null!,3); r.Should().HaveCount(3); r[0].Should().BeNull(); r[1].Should().BeNull(); r[2].Should().BeNull(); }
        [Fact] public void Fill_null_count() { var r=(object[])ArrayUdf.UDF_ARR_FILL("x",null!); r.Should().BeEmpty(); }
        [Fact] public void Fill_single() { var r=(object[])ArrayUdf.UDF_ARR_FILL("only",1); r.Should().Equal("only"); }
        [Fact] public void Fill_negative_count() => ArrayUdf.UDF_ARR_FILL("x",-1).Should().Be(ExcelError.Value);
        [Fact] public void Fill_boolean() { var r=(object[])ArrayUdf.UDF_ARR_FILL(true,3); r.Should().Equal(true,true,true); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.RANGE  (s, e, step → object[]) — ToDouble for all params
        //  step <= 0 defaults to 1
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Range_1_to_5() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1,5,1); r.Should().Equal(1.0,2.0,3.0,4.0,5.0); }
        [Fact] public void Range_with_step() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1.0,5.0,2.0); r.Should().Equal(1.0,3.0,5.0); }
        [Fact] public void Range_step_zero_defaults_1() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1,3,0); r.Should().Equal(1.0,2.0,3.0); }
        [Fact] public void Range_step_negative_defaults_1() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1,3,-1); r.Should().Equal(1.0,2.0,3.0); }
        [Fact] public void Range_start_gt_end() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(5,1,1); r.Should().BeEmpty(); }
        [Fact] public void Range_single() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(7,7,1); r.Should().Equal(7.0); }
        [Fact] public void Range_null_start() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(null!,5,1); r.Should().BeEmpty(); }
        [Fact] public void Range_large_step() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(0,9,3); r.Should().Equal(0.0,3.0,6.0,9.0); }
        [Fact] public void Range_exact_end() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1,10,3); r.Should().Equal(1.0,4.0,7.0,10.0); }
        [Fact] public void Range_doubles() { var r=(object[])ArrayUdf.UDF_ARR_RANGE(1.5,5.5,2.0); r.Should().Equal(1.5,3.5,5.5); }

        // ══════════════════════════════════════════════════════════════════
        //  ARR.SHUFFLE  (A(a) → Fisher-Yates shuffle in-place → object[])
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Shuffle_preserves_length() { var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(new object[]{1,2,3,4,5}); r.Should().HaveCount(5); }
        [Fact] public void Shuffle_preserves_elements() { var input=new object[]{1,2,3,4,5}; var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(input); r.Should().BeEquivalentTo(input); }
        [Fact] public void Shuffle_single() { var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(new object[]{42}); r.Should().Equal(42); }
        [Fact] public void Shuffle_empty() { var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(new object[0]); r.Should().BeEmpty(); }
        [Fact] public void Shuffle_null() { var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(null!); r.Should().BeEmpty(); }
        [Fact] public void Shuffle_strings() { var r=(object[])ArrayUdf.UDF_ARR_SHUFFLE(new object[]{"a","b","c","d"}); r.Should().HaveCount(4); r.Should().BeEquivalentTo(new object[]{"a","b","c","d"}); }
        [Fact] public void Shuffle_changes_order()
        {
            var input = new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var result = (object[])ArrayUdf.UDF_ARR_SHUFFLE(input);
            result.Should().NotBeEquivalentTo(input, o => o.WithStrictOrdering());
        }
    }
}
